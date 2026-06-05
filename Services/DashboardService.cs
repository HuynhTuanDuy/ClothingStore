using ClothingStore.Models.ViewModels;
using ClothingStore.Repositories;

namespace ClothingStore.Services;

public class DashboardService(IOrderRepository orderRepository) : IDashboardService
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
            RevenueLabels    = Enumerable.Range(1, 12)
                .Select(m => new DateTime(selectedYear, m, 1).ToString("MMM"))
                .ToList(),
            RevenueValues    = monthlyValues,
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
            }).ToList()
        };
    }
}
