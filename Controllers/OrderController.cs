using ClothingStore.Models.ViewModels;
using ClothingStore.Repositories;
using ClothingStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothingStore.Controllers;

/// <summary>Customer order history and detail (HIGH-05).</summary>
public class OrderController(
    IOrderRepository orderRepository,
    ICurrentCustomerService currentCustomerService) : Controller
{
    // ── GET /Order/History ──────────────────────────────────────
    public async Task<IActionResult> History()
    {
        var customerId = currentCustomerService.GetCustomerId();
        if (!customerId.HasValue)
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("History") });

        var orders = await orderRepository.GetOrdersByCustomerAsync(customerId.Value);

        var vm = new OrderHistoryViewModel
        {
            Orders = orders.Select(o => new OrderSummaryViewModel
            {
                OrderID       = o.OrderID,
                OrderCode     = o.OrderCode,
                OrderDate     = o.OrderDate,
                OrderStatus   = o.OrderStatus,
                PaymentMethod = o.PaymentMethod,
                PaymentStatus = o.PaymentStatus,
                FinalAmount   = o.FinalAmount,
                ItemCount     = o.OrderDetails.Count
            }).ToList()
        };

        return View(vm);
    }

    // ── GET /Order/Detail/{orderCode} ───────────────────────────
    public async Task<IActionResult> Detail(string orderCode)
    {
        if (string.IsNullOrWhiteSpace(orderCode)) return NotFound();

        var order = await orderRepository.GetOrderByCodeAsync(orderCode);
        if (order is null) return NotFound();

        // Prevent other customers from seeing the order
        var customerId = currentCustomerService.GetCustomerId();
        if (order.CustomerId.HasValue && order.CustomerId != customerId)
            return Forbid();

        var vm = new OrderDetailViewModel
        {
            OrderID        = order.OrderID,
            OrderCode      = order.OrderCode,
            TrackingNumber = order.TrackingNumber,
            OrderDate      = order.OrderDate,
            OrderEmail     = order.OrderEmail,
            ShippingName   = order.ShippingRecipientName,
            ShippingPhone  = order.ShippingPhone,
            ShippingAddress = string.IsNullOrWhiteSpace(order.ShippingFullAddress)
                ? $"{order.ShippingAddress}, {order.ShippingWard}, {order.ShippingDistrict}, {order.ShippingProvince}".Trim(',', ' ')
                : order.ShippingFullAddress,
            PaymentMethod  = order.PaymentMethod,
            PaymentStatus  = order.PaymentStatus,
            OrderStatus    = order.OrderStatus,
            SubTotal       = order.TotalAmount,
            ShippingFee    = order.ShippingFee,
            DiscountAmount = order.DiscountAmount,
            FinalAmount    = order.FinalAmount,
            CouponCode     = order.CouponUsages.FirstOrDefault()?.Coupon?.CouponCode,
            Items = order.OrderDetails.Select(d => new OrderDetailItemViewModel
            {
                ProductName = d.ProductNameSnapshot,
                SizeCode    = d.SizeCodeSnapshot,
                ColorName   = d.ColorNameSnapshot,
                SKU         = d.SKUSnapshot,
                UnitPrice   = d.UnitPrice,
                Quantity    = d.Quantity,
                SubTotal    = d.SubTotal
            }).ToList(),
            StatusHistory = order.StatusHistory
                .OrderByDescending(h => h.ChangedAt)
                .Select(h => new OrderStatusHistoryViewModel
                {
                    OldStatus = h.OldStatus,
                    NewStatus = h.NewStatus,
                    Note      = h.Note,
                    ChangedAt = h.ChangedAt
                }).ToList()
        };

        return View(vm);
    }
}
