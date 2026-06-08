using ClothingStore.Data;
using ClothingStore.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClothingStore.Repositories;

public record MonthlyRevenuePoint(int Month, decimal Revenue);
public record TopProductPoint(string ProductName, int TotalSold, decimal Revenue);
public record RecentOrderPoint(
    string OrderCode, DateTime OrderDate,
    string CustomerName, string OrderEmail,
    decimal FinalAmount, string OrderStatus, string PaymentMethod);
public record LowStockPoint(
    string SKU, string ProductName,
    string SizeCode, string ColorName, int StockQuantity);
public record LowStockProductPoint(
    int ProductID, string ProductName, string ThumbnailUrl, 
    int TotalStock, List<LowStockPoint> Variants);
public record CategorySalesPoint(string CategoryName, int TotalSold);

public class OrderRepository(StoreDbContext dbContext) : IOrderRepository
{
    public async Task AddOrderAsync(Order order)
    {
        await dbContext.Orders.AddAsync(order);
    }

    public Task<Order?> GetOrderByCodeAsync(string orderCode)
    {
        return dbContext.Orders
            .AsNoTracking()
            .Include(x => x.OrderDetails)
            .Include(x => x.StatusHistory.OrderByDescending(h => h.ChangedAt))
            .Include(x => x.CouponUsages).ThenInclude(x => x.Coupon)
            .FirstOrDefaultAsync(x => x.OrderCode == orderCode);
    }

    public Task<Order?> GetOrderByIdAsync(int orderId)
    {
        return dbContext.Orders
            .Include(x => x.OrderDetails)
                .ThenInclude(od => od.ProductVariant)
                    .ThenInclude(pv => pv.Product)
            .Include(x => x.StatusHistory.OrderByDescending(h => h.ChangedAt))
            .Include(x => x.CouponUsages).ThenInclude(x => x.Coupon)
            .Include(x => x.Customer).ThenInclude(c => c!.Membership)
            .FirstOrDefaultAsync(x => x.OrderID == orderId);
    }

    public Task<List<Order>> GetOrdersByCustomerAsync(int customerId)
    {
        return dbContext.Orders
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .Include(x => x.OrderDetails)
            .OrderByDescending(x => x.OrderDate)
            .ToListAsync();
    }

    public async Task<List<Order>> GetAllOrdersAsync(string? status = null, int? day = null, int? month = null, int? year = null, int page = 1, int pageSize = 20)
    {
        var query = dbContext.Orders
            .AsNoTracking()
            .Include(x => x.Customer)
                .ThenInclude(c => c!.Membership)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.OrderStatus == status);

        if (year.HasValue)
            query = query.Where(x => x.OrderDate.Year == year.Value);
        
        if (month.HasValue)
            query = query.Where(x => x.OrderDate.Month == month.Value);
            
        if (day.HasValue)
            query = query.Where(x => x.OrderDate.Day == day.Value);

        return await query
            .OrderByDescending(x => x.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public Task<int> CountOrdersAsync(int year)
    {
        var start = new DateTime(year, 1, 1);
        var end = start.AddYears(1);
        return dbContext.Orders
            .AsNoTracking()
            .CountAsync(x => x.OrderDate >= start && x.OrderDate < end);
    }

    public Task<int> CountPendingOrdersAsync()
    {
        return dbContext.Orders
            .AsNoTracking()
            .CountAsync(x => x.OrderStatus == OrderStatus.Pending);
    }

    /// <summary>
    /// [BUG-01 FIX] Load filtered data then group on client to avoid EF Core LINQ translation issue.
    /// </summary>
    public async Task<List<MonthlyRevenuePoint>> GetMonthlyRevenueAsync(int year)
    {
        var start = new DateTime(year, 1, 1);
        var end = start.AddYears(1);

        // Pull only Month + FinalAmount — minimal data transfer
        var rows = await dbContext.Orders
            .AsNoTracking()
            .Where(x => x.OrderDate >= start && x.OrderDate < end
                     && x.PaymentStatus == PaymentStatus.Paid)
            .Select(x => new { x.OrderDate.Month, x.FinalAmount })
            .ToListAsync();

        return rows
            .GroupBy(x => x.Month)
            .Select(g => new MonthlyRevenuePoint(g.Key, g.Sum(x => x.FinalAmount)))
            .OrderBy(x => x.Month)
            .ToList();
    }

    public async Task<List<TopProductPoint>> GetTopProductsAsync(int year, int top = 10)
    {
        var start = new DateTime(year, 1, 1);
        var end = start.AddYears(1);

        var rows = await dbContext.OrderDetails
            .AsNoTracking()
            .Where(x => x.Order.OrderDate >= start && x.Order.OrderDate < end
                     && x.Order.PaymentStatus == PaymentStatus.Paid)
            .Select(x => new
            {
                // Use snapshot name — won't break even if product is deleted
                ProductName = x.ProductNameSnapshot == string.Empty
                    ? x.ProductVariant.Product.ProductName
                    : x.ProductNameSnapshot,
                x.Quantity,
                SubTotal = x.UnitPrice * x.Quantity
            })
            .ToListAsync();

        return rows
            .GroupBy(x => x.ProductName)
            .Select(g => new TopProductPoint(g.Key, g.Sum(x => x.Quantity), g.Sum(x => x.SubTotal)))
            .OrderByDescending(x => x.TotalSold)
            .Take(top)
            .ToList();
    }

    public Task<int> CountLowStockVariantsAsync(int threshold = 5)
    {
        return dbContext.ProductVariants
            .AsNoTracking()
            .CountAsync(x => x.IsActive && x.StockQuantity <= threshold);
    }

    public Task<int> CountCustomersAsync()
    {
        return dbContext.Customers.AsNoTracking().CountAsync();
    }

    public async Task<List<RecentOrderPoint>> GetRecentOrdersAsync(int count = 8)
    {
        var rows = await dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .OrderByDescending(o => o.OrderDate)
            .Take(count)
            .Select(o => new
            {
                o.OrderCode,
                o.OrderDate,
                CustomerName = o.Customer != null ? o.Customer.FullName : o.ShippingRecipientName,
                o.OrderEmail,
                o.FinalAmount,
                o.OrderStatus,
                o.PaymentMethod
            })
            .ToListAsync();

        return rows.Select(r => new RecentOrderPoint(
            r.OrderCode, r.OrderDate, r.CustomerName,
            r.OrderEmail, r.FinalAmount, r.OrderStatus, r.PaymentMethod))
            .ToList();
    }

    public async Task<List<LowStockPoint>> GetLowStockItemsAsync(int threshold = 5, int count = 8)
    {
        var rows = await dbContext.ProductVariants
            .AsNoTracking()
            .Where(v => v.IsActive && v.StockQuantity <= threshold)
            .Include(v => v.Product)
            .Include(v => v.Size)
            .Include(v => v.Color)
            .OrderBy(v => v.StockQuantity)
            .Take(count)
            .Select(v => new
            {
                v.SKU,
                ProductName = v.Product.ProductName,
                SizeCode    = v.Size.SizeCode,
                ColorName   = v.Color.ColorName,
                v.StockQuantity
            })
            .ToListAsync();

        return rows.Select(r => new LowStockPoint(
            r.SKU, r.ProductName, r.SizeCode, r.ColorName, r.StockQuantity))
            .ToList();
    }
    public Task<int> CountShippingOrdersAsync()
    {
        return dbContext.Orders
            .AsNoTracking()
            .CountAsync(x => x.OrderStatus == OrderStatus.Shipping);
    }

    public async Task<double> CalculateCompletionRateAsync()
    {
        var total = await dbContext.Orders.AsNoTracking().CountAsync();
        if (total == 0) return 0;
        var delivered = await dbContext.Orders.AsNoTracking().CountAsync(x => x.OrderStatus == OrderStatus.Delivered);
        return Math.Round((double)delivered / total * 100, 1);
    }

    public Task<int> TotalProductsSoldAsync()
    {
        return dbContext.OrderDetails
            .AsNoTracking()
            .Where(x => x.Order.OrderStatus != OrderStatus.Cancelled)
            .SumAsync(x => x.Quantity);
    }

    public async Task<List<LowStockProductPoint>> GetLowStockProductsAsync(int threshold = 5)
    {
        var lowStockVariants = await dbContext.ProductVariants
            .AsNoTracking()
            .Where(v => v.IsActive && v.StockQuantity <= threshold)
            .Select(v => v.ProductID)
            .Distinct()
            .ToListAsync();

        var products = await dbContext.Products
            .AsNoTracking()
            .Include(p => p.ProductVariants)
                .ThenInclude(v => v.Size)
            .Include(p => p.ProductVariants)
                .ThenInclude(v => v.Color)
            .Include(p => p.ProductVariants)
                .ThenInclude(v => v.ProductImages)
            .Where(p => lowStockVariants.Contains(p.ProductID))
            .ToListAsync();

        return products.Select(p => {
            var mainImage = p.ProductVariants.SelectMany(v => v.ProductImages).FirstOrDefault(i => i.IsMain)?.ImageURL;
            var thumb = !string.IsNullOrEmpty(p.ThumbnailUrl) ? p.ThumbnailUrl : (mainImage ?? "");
            
            return new LowStockProductPoint(
                p.ProductID,
                p.ProductName,
                thumb,
                p.ProductVariants.Where(v => v.IsActive).Sum(v => v.StockQuantity),
                p.ProductVariants
                    .Where(v => v.IsActive)
                    .Select(v => new LowStockPoint(v.SKU, p.ProductName, v.Size.SizeCode, v.Color.ColorName, v.StockQuantity))
                    .OrderBy(v => v.StockQuantity)
                    .ToList()
            );
        }).OrderBy(p => p.TotalStock).ToList();
    }
    public async Task<List<CategorySalesPoint>> GetSalesByCategoryAsync(int year)
    {
        var start = new DateTime(year, 1, 1);
        var end = start.AddYears(1);

        var rows = await dbContext.OrderDetails
            .AsNoTracking()
            .Where(x => x.Order.OrderDate >= start && x.Order.OrderDate < end
                     && x.Order.PaymentStatus == PaymentStatus.Paid)
            .Select(x => new
            {
                CategoryName = x.ProductVariant.Product.Category.CategoryName,
                x.Quantity
            })
            .ToListAsync();

        return rows
            .GroupBy(x => x.CategoryName)
            .Select(g => new CategorySalesPoint(g.Key, g.Sum(x => x.Quantity)))
            .OrderByDescending(x => x.TotalSold)
            .ToList();
    }

    public async Task<Order?> GetOrderForGuestTrackingAsync(string orderCode, string phone)
    {
        return await dbContext.Orders
            .Include(o => o.StatusHistory.OrderByDescending(sh => sh.ChangedAt))
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ProductVariant)
                    .ThenInclude(pv => pv.Product)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ProductVariant)
                    .ThenInclude(pv => pv.Color)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ProductVariant)
                    .ThenInclude(pv => pv.Size)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ProductVariant)
                    .ThenInclude(pv => pv.ProductImages)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode && o.ShippingPhone == phone);
    }
}
