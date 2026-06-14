using ClothingStore.Models.Entities;

namespace ClothingStore.Repositories;

public interface IOrderRepository
{
    Task AddOrderAsync(Order order);
    Task<Order?> GetOrderByCodeAsync(string orderCode);
    Task<Order?> GetOrderByIdAsync(int orderId);
    Task<List<Order>> GetOrdersByCustomerAsync(int customerId);
    Task<List<Order>> GetAllOrdersAsync(string? status = null, int? day = null, int? month = null, int? year = null, int page = 1, int pageSize = 20);
    Task<int> CountOrdersAsync(int year);
    Task<int> CountPendingOrdersAsync();
    Task<int> CountCustomersAsync();
    Task<List<MonthlyRevenuePoint>> GetMonthlyRevenueAsync(int year);
    Task<List<TopProductPoint>> GetTopProductsAsync(int year, int top = 10);
    Task<int> CountLowStockVariantsAsync(int threshold = 5);
    Task<List<RecentOrderPoint>> GetRecentOrdersAsync(int count = 8);
    Task<List<LowStockPoint>> GetLowStockItemsAsync(int threshold = 5, int count = 8);
    Task<int> CountShippingOrdersAsync();
    Task<double> CalculateCompletionRateAsync();
    Task<int> TotalProductsSoldAsync();
    Task<int> CountRetryWaitingOrdersAsync();
    Task<int> CountMaxAttemptsExceededOrdersAsync();
    Task<List<TopFailureReasonPoint>> GetTopFailureReasonsAsync(int count = 5);
    Task<List<LowStockProductPoint>> GetLowStockProductsAsync(int threshold = 5);
    Task<List<CategorySalesPoint>> GetSalesByCategoryAsync(int year);
    Task<Order?> GetOrderForGuestTrackingAsync(string orderCode, string phone);
    Task<List<Order>> GetOrdersByShipperAsync(int shipperId, string? status = null);
    Task<Order?> GetOrderDetailForShipperAsync(int orderId, int shipperId);
}
