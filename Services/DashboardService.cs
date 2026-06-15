using ClothingStore.Models.ViewModels;
using ClothingStore.Repositories;
using ClothingStore.Data;
using Microsoft.EntityFrameworkCore;

namespace ClothingStore.Services;

public class DashboardService(IOrderRepository orderRepository, StoreDbContext dbContext) : IDashboardService
{
    private const int LowStockThreshold = 5;

    public async Task<DashboardViewModel> GetDashboardAsync(int? year = null)
    {
        var selectedYear = year ?? DateTime.UtcNow.Year;

        // Execute queries sequentially to avoid EF Core concurrent DbContext usage exceptions
        var revenue        = await orderRepository.GetMonthlyRevenueAsync(selectedYear);
        var topProducts    = await orderRepository.GetTopProductsAsync(selectedYear, top: 8);
        var orderCount     = await orderRepository.CountOrdersAsync(selectedYear);
        var pending        = await orderRepository.CountPendingOrdersAsync();
        var customerCount  = await orderRepository.CountCustomersAsync();
        var lowStockCount  = await orderRepository.CountLowStockVariantsAsync(LowStockThreshold);
        var recentOrders   = await orderRepository.GetRecentOrdersAsync(count: 8);
        var lowStockItems  = await orderRepository.GetLowStockItemsAsync(LowStockThreshold, count: 8);
        
        // New dynamic stats
        var shippingOrders = await orderRepository.CountShippingOrdersAsync();
        var completionRate = await orderRepository.CalculateCompletionRateAsync();
        var totalProductsSold = await orderRepository.TotalProductsSoldAsync();
        var lowStockProducts = await orderRepository.GetLowStockProductsAsync(LowStockThreshold);
        var categorySales = await orderRepository.GetSalesByCategoryAsync(selectedYear);
        var orderStatusCounts = await orderRepository.GetOrderStatusCountsAsync(selectedYear);
        var retryWaitingCount = await orderRepository.CountRetryWaitingOrdersAsync();
        var maxAttemptsExceededCount = await orderRepository.CountMaxAttemptsExceededOrdersAsync();
        var topFailureReasons = await orderRepository.GetTopFailureReasonsAsync(selectedYear);

        // Top Searches with No Results
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var topSearchesNoResult = await dbContext.SearchLogs
            .Where(x => x.SearchedAt >= thirtyDaysAgo && x.ResultCount == 0 && x.Keyword != null && x.Keyword.Length >= 2)
            .GroupBy(x => x.Keyword)
            .Select(g => new TopSearchNoResultViewModel
            {
                Keyword = g.Key,
                SearchCount = g.Count()
            })
            .OrderByDescending(x => x.SearchCount)
            .Take(10)
            .ToListAsync();

        var rawTopSearches = await dbContext.SearchLogs
            .Where(x => x.SearchedAt >= thirtyDaysAgo && x.Keyword != null && x.Keyword.Length >= 2)
            .GroupBy(x => x.Keyword)
            .Select(g => new
            {
                Keyword = g.Key,
                SearchCount = g.Count(),
                ClickCount = g.Count(x => x.ClickedProductId != null)
            })
            .OrderByDescending(x => x.SearchCount)
            .Take(100)
            .ToListAsync();

        var topSearches = rawTopSearches
            .Select(x => new TopSearchViewModel
            {
                Keyword = x.Keyword,
                SearchCount = x.SearchCount,
                ClickCount = x.ClickCount
            })
            .OrderByDescending(x => x.SearchCount)
            .ThenByDescending(x => x.Ctr)
            .Take(20)
            .ToList();

        var monthlyValues = Enumerable.Range(1, 12)
            .Select(m => revenue.FirstOrDefault(x => x.Month == m)?.Revenue ?? 0m)
            .ToList();

        return new DashboardViewModel
        {
            Year             = selectedYear,
            TotalOrders      = orderCount,
            TotalRevenue     = monthlyValues.Sum(),
            PendingOrders    = pending,
            TotalCustomers   = customerCount,
            LowStockVariants = lowStockCount,
            
            ShippingOrders   = shippingOrders,
            CompletionRate   = completionRate,
            TotalProductsSold = totalProductsSold,
            RetryWaitingCount = retryWaitingCount,
            MaxAttemptsExceededCount = maxAttemptsExceededCount,

            RevenueLabels    = Enumerable.Range(1, 12)
                .Select(m => new DateTime(selectedYear, m, 1).ToString("MMM"))
                .ToList(),
            RevenueValues    = monthlyValues,
            CategorySalesLabels = categorySales.Select(x => x.CategoryName).ToList(),
            CategorySalesValues = categorySales.Select(x => x.TotalSold).ToList(),
            OrderStatusLabels   = orderStatusCounts.Select(x => x.Status).ToList(),
            OrderStatusValues   = orderStatusCounts.Select(x => x.Count).ToList(),
            TopProducts      = topProducts.Select(x => new TopProductViewModel
            {
                ProductName = x.ProductName,
                TotalSold   = x.TotalSold,
                Revenue     = x.Revenue
            }).ToList(),
            RecentOrders = recentOrders.Select(r => new RecentOrderViewModel
            {
                OrderCode    = r.OrderCode,
                OrderDate    = r.OrderDate,
                CustomerName = r.CustomerName,
                OrderEmail   = r.OrderEmail,
                FinalAmount  = r.FinalAmount,
                OrderStatus  = r.OrderStatus,
                PaymentMethod = r.PaymentMethod
            }).ToList(),
            LowStockItems = lowStockItems.Select(r => new LowStockItemViewModel
            {
                SKU           = r.SKU,
                ProductName   = r.ProductName,
                SizeCode      = r.SizeCode,
                ColorName     = r.ColorName,
                StockQuantity = r.StockQuantity
            }).ToList(),
            LowStockProducts = lowStockProducts.Select(p => new LowStockProductViewModel
            {
                ProductID = p.ProductID,
                ProductName = p.ProductName,
                ThumbnailUrl = p.ThumbnailUrl,
                TotalStock = p.TotalStock,
                Variants = p.Variants.Select(v => new LowStockVariantViewModel
                {
                    SKU = v.SKU,
                    SizeCode = v.SizeCode,
                    ColorName = v.ColorName,
                    StockQuantity = v.StockQuantity
                }).ToList()
            }).ToList(),
            TopFailureReasons = topFailureReasons.Select(r => new TopFailureReasonViewModel
            {
                Reason = r.Reason,
                Count = r.Count
            }).ToList(),
            TopSearchesNoResult = topSearchesNoResult,
            TopSearches = topSearches
        };
    }
}
