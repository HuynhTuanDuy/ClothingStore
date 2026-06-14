using ClothingStore.Models.Entities;
using ClothingStore.Models.ViewModels;

namespace ClothingStore.Services;

public interface IShipperService
{
    Task<ShipperDashboardViewModel> GetDashboardAsync(int shipperId);
    Task<List<ShipperOrderViewModel>> GetOrdersAsync(int shipperId, string? status);
    Task<ShipperOrderDetailViewModel?> GetOrderDetailAsync(int orderId, int shipperId);
    Task<(bool Success, string Message)> UpdateDeliveryStatusAsync(int orderId, int shipperId, DeliveryStatusUpdateViewModel model);
}
