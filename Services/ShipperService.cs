using ClothingStore.Data;
using ClothingStore.Models.Entities;
using ClothingStore.Models.ViewModels;
using ClothingStore.Repositories;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Configuration;

namespace ClothingStore.Services;

public class ShipperService(StoreDbContext dbContext, IOrderRepository orderRepository, IConfiguration configuration, IDateTimeService dateTimeService) : IShipperService
{
    private int MaxDeliveryAttempts => configuration.GetValue<int>("DeliverySettings:MaxDeliveryAttempts", 3);

    public async Task<ShipperDashboardViewModel> GetDashboardAsync(int shipperId)
    {
        var utcNow = dateTimeService.UtcNow;
        var vnTime = dateTimeService.ConvertUtcToLocal(utcNow);
        
        // Ensure EF Core queries use the UTC equivalent of local start/end times.
        var startOfDayVnUtc = dateTimeService.ConvertLocalToUtc(vnTime.Date);
        var endOfDayVnUtc = startOfDayVnUtc.AddDays(1);
        
        var startOfMonthVnUtc = dateTimeService.ConvertLocalToUtc(new DateTime(vnTime.Year, vnTime.Month, 1));

        var orders = await dbContext.Orders
            .AsNoTracking()
            .Where(o => o.AssignedShipperId == shipperId)
            .ToListAsync();

        var deliveredToday = orders.Count(o => o.OrderStatus == OrderStatus.Delivered && o.DeliveredAt.HasValue && o.DeliveredAt.Value >= startOfDayVnUtc && o.DeliveredAt.Value < endOfDayVnUtc);
        var deliveredMonth = orders.Count(o => o.OrderStatus == OrderStatus.Delivered && o.DeliveredAt.HasValue && o.DeliveredAt.Value >= startOfMonthVnUtc);
        
        var completedCount = orders.Count(o => o.OrderStatus == OrderStatus.Delivered);
        var failedCountAll = orders.Count(o => o.OrderStatus == OrderStatus.DeliveryFailed);
        
        double successRate = 0;
        if (completedCount + failedCountAll > 0)
        {
            successRate = Math.Round((double)completedCount / (completedCount + failedCountAll) * 100, 1);
        }

        var longestOrder = orders
            .Where(o => o.OrderStatus == OrderStatus.Shipping && o.ShippingStartedAt.HasValue)
            .OrderBy(o => o.ShippingStartedAt)
            .FirstOrDefault();

        var upcomingRetryCount = orders.Count(o => o.OrderStatus == OrderStatus.DeliveryAttemptFailed 
                                                   && o.NextDeliveryDate.HasValue 
                                                   && o.NextDeliveryDate.Value <= endOfDayVnUtc.AddDays(1)
                                                   && o.NextDeliveryDate.Value > utcNow);

        var overdueRetryCount = orders.Count(o => o.OrderStatus == OrderStatus.DeliveryAttemptFailed
                                                  && o.NextDeliveryDate.HasValue
                                                  && o.NextDeliveryDate.Value <= utcNow);

        return new ShipperDashboardViewModel
        {
            WaitingCount = orders.Count(o => o.OrderStatus == OrderStatus.ReadyToShip),
            ShippingCount = orders.Count(o => o.OrderStatus == OrderStatus.Shipping),
            DeliveredTodayCount = deliveredToday,
            FailedCount = orders.Count(o => o.OrderStatus == OrderStatus.DeliveryFailed),
            MonthlyDeliveredCount = deliveredMonth,
            CodPendingCount = orders.Count(o => o.OrderStatus == OrderStatus.DeliveredPendingCOD),
            SuccessRate = successRate,
            LongestShippingOrderCode = longestOrder?.OrderCode,
            LongestShippingDurationDays = longestOrder?.ShippingStartedAt.HasValue == true ? (int)(utcNow - longestOrder.ShippingStartedAt.Value).TotalDays : 0,
            RetryWaitingCount = orders.Count(o => o.OrderStatus == OrderStatus.DeliveryAttemptFailed),
            UpcomingRetryCount = upcomingRetryCount,
            OverdueRetryCount = overdueRetryCount
        };
    }

    public async Task<List<ShipperOrderViewModel>> GetOrdersAsync(int shipperId, string? status)
    {
        var orders = await orderRepository.GetOrdersByShipperAsync(shipperId, status);
        
        return orders.Select(o => new ShipperOrderViewModel
        {
            OrderID = o.OrderID,
            OrderCode = o.OrderCode,
            CustomerName = o.Customer?.FullName ?? o.ShippingRecipientName,
            Phone = string.IsNullOrEmpty(o.ShippingPhone) ? (o.Customer?.Phone ?? "") : o.ShippingPhone,
            Address = string.IsNullOrWhiteSpace(o.ShippingFullAddress) 
                ? $"{o.ShippingAddress}, {o.ShippingWard}, {o.ShippingDistrict}, {o.ShippingProvince}".Trim(',', ' ') 
                : o.ShippingFullAddress,
            FinalAmount = o.FinalAmount,
            Status = o.OrderStatus,
            AssignedAt = o.AssignedAt,
            AssignedAtLocal = o.AssignedAt.HasValue ? dateTimeService.ConvertUtcToLocal(o.AssignedAt.Value) : null,
            NextDeliveryDate = o.NextDeliveryDate,
            NextDeliveryDateLocal = o.NextDeliveryDate.HasValue ? dateTimeService.ConvertUtcToLocal(o.NextDeliveryDate.Value) : null,
            DeliveryAttemptCount = o.DeliveryAttemptCount
        }).ToList();
    }

    public async Task<ShipperOrderDetailViewModel?> GetOrderDetailAsync(int orderId, int shipperId)
    {
        var order = await orderRepository.GetOrderDetailForShipperAsync(orderId, shipperId);
        if (order == null) return null;

        return new ShipperOrderDetailViewModel
        {
            OrderID = order.OrderID,
            OrderCode = order.OrderCode,
            CustomerName = order.Customer?.FullName ?? order.ShippingRecipientName,
            Phone = string.IsNullOrEmpty(order.ShippingPhone) ? (order.Customer?.Phone ?? "") : order.ShippingPhone,
            Address = string.IsNullOrWhiteSpace(order.ShippingFullAddress) 
                ? $"{order.ShippingAddress}, {order.ShippingWard}, {order.ShippingDistrict}, {order.ShippingProvince}".Trim(',', ' ') 
                : order.ShippingFullAddress,
            FinalAmount = order.FinalAmount,
            Status = order.OrderStatus,
            AssignedAt = order.AssignedAt,
            AssignedAtLocal = order.AssignedAt.HasValue ? dateTimeService.ConvertUtcToLocal(order.AssignedAt.Value) : null,
            OrderDate = order.OrderDate,
            OrderDateLocal = dateTimeService.ConvertUtcToLocal(order.OrderDate),
            PaymentMethod = order.PaymentMethod,
            PaymentStatus = order.PaymentStatus,
            ShippingFee = order.ShippingFee,
            TotalAmount = order.TotalAmount,
            DeliveryFailureReasonCode = order.DeliveryFailureReasonCode,
            DeliveryFailureReason = order.DeliveryFailureReason,
            Items = order.OrderDetails.Select(od => new OrderDetailItemViewModel
            {
                ProductName = od.ProductNameSnapshot == string.Empty ? od.ProductVariant.Product.ProductName : od.ProductNameSnapshot,
                SizeCode = od.SizeCodeSnapshot == string.Empty ? od.ProductVariant.Size.SizeCode : od.SizeCodeSnapshot,
                ColorName = od.ColorNameSnapshot == string.Empty ? od.ProductVariant.Color.ColorName : od.ColorNameSnapshot,
                SKU = od.SKUSnapshot == string.Empty ? od.ProductVariant.SKU : od.SKUSnapshot,
                UnitPrice = od.UnitPrice,
                Quantity = od.Quantity,
                SubTotal = od.SubTotal,
                ImageUrl = od.ProductVariant.ProductImages.FirstOrDefault(i => i.IsMain)?.ImageURL
            }).ToList(),
            DeliveryAttemptCount = order.DeliveryAttemptCount,
            NextDeliveryDate = order.NextDeliveryDate,
            NextDeliveryDateLocal = order.NextDeliveryDate.HasValue ? dateTimeService.ConvertUtcToLocal(order.NextDeliveryDate.Value) : null,
            DeliveryRescheduleReason = order.DeliveryRescheduleReason
        };
    }

    public async Task<(bool Success, string Message)> UpdateDeliveryStatusAsync(int orderId, int shipperId, DeliveryStatusUpdateViewModel model)
    {
        var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.OrderID == orderId);
        if (order == null) return (false, "Order not found.");
        
        // Ownership / Admin check. We only know shipperId, meaning if the caller is Shipper, it must match.
        // Assuming admin bypasses this or passes shipperId = order.AssignedShipperId? 
        // Wait, the interface gets called by ShipperController. ShipperController passes CurrentUserId as shipperId.
        // So this check is for Shipper. 
        // We will add an 'isAdmin' flag or similar if needed, but for now we keep the check.
        if (order.AssignedShipperId != shipperId) return (false, "Forbidden: Not assigned to you.");

        var oldStatus = order.OrderStatus;
        var newStatus = model.NewStatus;

        // COD Flow logic
        if (newStatus == OrderStatus.Delivered && order.PaymentMethod == PaymentMethod.COD)
        {
            newStatus = OrderStatus.DeliveredPendingCOD;
        }

        // Validation for allowed transitions
        bool isAllowed = false;
        if (oldStatus == OrderStatus.ReadyToShip && newStatus == OrderStatus.Shipping) isAllowed = true;
        if (oldStatus == OrderStatus.Shipping && newStatus == OrderStatus.Delivered) isAllowed = true;
        if (oldStatus == OrderStatus.Shipping && newStatus == OrderStatus.DeliveredPendingCOD) isAllowed = true;
        if (oldStatus == OrderStatus.Shipping && newStatus == OrderStatus.DeliveryFailed) isAllowed = true;
        if (oldStatus == OrderStatus.Shipping && newStatus == OrderStatus.DeliveryAttemptFailed) isAllowed = true;
        if (oldStatus == OrderStatus.DeliveryAttemptFailed && newStatus == OrderStatus.Shipping) isAllowed = true;

        if (!isAllowed)
        {
            return (false, $"Invalid status transition from {oldStatus} to {newStatus}.");
        }

        if (newStatus == OrderStatus.DeliveryAttemptFailed)
        {
            if (model.NextDeliveryDate == null)
            {
                return (false, "NextDeliveryDate is required when rescheduling.");
            }
        }

        if (newStatus == OrderStatus.Shipping && oldStatus == OrderStatus.DeliveryAttemptFailed)
        {
            if (order.NextDeliveryDate.HasValue && dateTimeService.UtcNow < order.NextDeliveryDate.Value)
            {
                return (false, "Chưa đến thời gian giao lại.");
            }
            if (order.DeliveryAttemptCount >= MaxDeliveryAttempts)
            {
                return (false, "Đã vượt quá số lần giao lại cho phép.");
            }
        }

        if (newStatus == OrderStatus.DeliveryFailed && string.IsNullOrWhiteSpace(model.ReasonCode))
        {
            return (false, "ReasonCode is required when marking as DeliveryFailed.");
        }

        // Apply changes
        order.OrderStatus = newStatus;
        string actionType = "StatusChanged";

        if (newStatus == OrderStatus.Shipping)
        {
            if (oldStatus == OrderStatus.DeliveryAttemptFailed)
            {
                actionType = "ResumeDelivery";
            }
            else
            {
                order.ShippingStartedAt = dateTimeService.UtcNow;
                actionType = "StartShipping";
            }
        }
        else if (newStatus == OrderStatus.Delivered)
        {
            order.DeliveredAt = dateTimeService.UtcNow;
            actionType = "Delivered";
        }
        else if (newStatus == OrderStatus.DeliveredPendingCOD)
        {
            order.DeliveredAt = dateTimeService.UtcNow; // Logically delivered by shipper
            actionType = "DeliveredPendingCOD";
        }
        else if (newStatus == OrderStatus.DeliveryFailed)
        {
            order.DeliveryFailureReasonCode = model.ReasonCode;
            order.DeliveryFailureReason = model.Reason;
            if (order.PaymentMethod == PaymentMethod.COD)
            {
                order.OrderStatus = OrderStatus.Returned;
            }
            actionType = "DeliveryFailed";
        }
        else if (newStatus == OrderStatus.DeliveryAttemptFailed)
        {
            order.DeliveryAttemptCount++;
            if (model.NextDeliveryDate.HasValue)
            {
                order.NextDeliveryDate = dateTimeService.ConvertLocalToUtc(model.NextDeliveryDate.Value);
            }
            order.DeliveryRescheduleReason = model.Reason;
            order.LastDeliveryAttemptAt = dateTimeService.UtcNow;
            actionType = "DeliveryAttemptFailed";
            
            // Auto close if exceed
            if (order.DeliveryAttemptCount >= MaxDeliveryAttempts)
            {
                var failedHistory = new OrderStatusHistory
                {
                    OrderID = order.OrderID,
                    OldStatus = oldStatus,
                    NewStatus = OrderStatus.DeliveryAttemptFailed,
                    ActionType = "DeliveryAttemptFailed",
                    Note = model.Reason,
                    ChangedAt = dateTimeService.UtcNow,
                    ChangedBy = shipperId
                };
                dbContext.OrderStatusHistory.Add(failedHistory);

                oldStatus = OrderStatus.DeliveryAttemptFailed;
                actionType = "DeliveryAttemptsExceeded";
                order.DeliveryFailureReasonCode = "MaxAttemptsExceeded";
                order.DeliveryFailureReason = "Vượt quá số lần giao lại";
                if (order.PaymentMethod == PaymentMethod.COD)
                {
                    order.OrderStatus = OrderStatus.Returned;
                }
                else
                {
                    order.OrderStatus = OrderStatus.DeliveryFailed;
                }
                newStatus = order.OrderStatus;
            }
        }

        var history = new OrderStatusHistory
        {
            OrderID = order.OrderID,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ActionType = actionType,
            Note = model.Reason,
            ChangedAt = dateTimeService.UtcNow,
            ChangedBy = shipperId
        };
        
        dbContext.OrderStatusHistory.Add(history);
        await dbContext.SaveChangesAsync();

        return (true, "Status updated successfully.");
    }
}
