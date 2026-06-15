using ClothingStore.Models.ViewModels;
using ClothingStore.Repositories;
using ClothingStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;


using ClothingStore.Attributes;
using Microsoft.EntityFrameworkCore;

namespace ClothingStore.Areas.Admin.Controllers;

[Area("Admin")]
[RequirePermission("Order.View")]
public class OrdersController(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork) : Controller
{
    private const int SHIPPER_OVERLOAD_THRESHOLD = 20;

    private static readonly List<string> AllStatuses = [
        Models.Entities.OrderStatus.Pending,
        Models.Entities.OrderStatus.Confirmed,
        Models.Entities.OrderStatus.Processing,
        Models.Entities.OrderStatus.ReadyToShip,
        Models.Entities.OrderStatus.Shipping,
        Models.Entities.OrderStatus.DeliveryAttemptFailed,
        Models.Entities.OrderStatus.DeliveryFailed,
        Models.Entities.OrderStatus.DeliveredPendingCOD,
        Models.Entities.OrderStatus.Delivered,
        Models.Entities.OrderStatus.Returned,
        Models.Entities.OrderStatus.Cancelled
    ];

    // ── GET /Admin/Orders ───────────────────────────────────────
    public async Task<IActionResult> Index(string? status = null, int? day = null, int? month = null, int? year = null, int page = 1)
    {
        var orders = await orderRepository.GetAllOrdersAsync(status, day, month, year, page);
        // Total count query (simplified)
        var allOrders = await orderRepository.GetAllOrdersAsync(status, day, month, year, 1, 10000);

        var shippingOrders = await orderRepository.CountShippingOrdersAsync();
        var completionRate = await orderRepository.CalculateCompletionRateAsync();
        var totalProductsSold = await orderRepository.TotalProductsSoldAsync();

        var vm = new AdminOrderListViewModel
        {
            StatusFilter = status,
            FilterDay    = day,
            FilterMonth  = month,
            FilterYear   = year,
            Page         = page,
            TotalCount   = allOrders.Count,
            ShippingOrders = shippingOrders,
            CompletionRate = completionRate,
            TotalProductsSold = totalProductsSold,
            Orders = orders.Select(o => new AdminOrderSummaryViewModel
            {
                OrderID       = o.OrderID,
                OrderCode     = o.OrderCode,
                OrderDate     = o.OrderDate,
                CustomerName  = o.Customer?.FullName ?? (!string.IsNullOrWhiteSpace(o.ShippingRecipientName) ? o.ShippingRecipientName : "Khách vãng lai"),
                OrderEmail    = o.OrderEmail,
                OrderStatus   = o.OrderStatus,
                PaymentMethod = o.PaymentMethod,
                PaymentStatus = o.PaymentStatus,
                FinalAmount   = o.FinalAmount,
                ItemCount     = o.OrderDetails.Count,
                MembershipRank = o.Customer?.Membership?.MembershipName ?? "Khách vãng lai"
            }).ToList()
        };

        return View(vm);
    }

    public async Task<IActionResult> ExportCsv([FromServices] ClothingStore.Data.StoreDbContext context)
    {
        var orders = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.Include(context.Orders, o => o.Customer)
        );

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("OrderID,OrderCode,CustomerName,OrderStatus,TotalAmount,OrderDate");

        foreach (var o in orders)
        {
            var customerName = $"\"{o.Customer?.FullName?.Replace("\"", "\"\"")}\"";
            var totalAmount = o.TotalAmount.ToString("N0").Replace(",", "");
            var orderDate = o.OrderDate.ToString("dd/MM/yyyy ss:mm:HH");

            sb.AppendLine($"{o.OrderID},{o.OrderCode},{customerName},{o.OrderStatus},{totalAmount},{orderDate}");
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "OrdersReport.csv");
    }

    // ── GET /Admin/Orders/Detail/{id} ───────────────────────────
    public async Task<IActionResult> Detail(int id, [FromServices] ClothingStore.Data.StoreDbContext context, [FromServices] IDateTimeService dateTimeService)
    {
        var order = await orderRepository.GetOrderByIdAsync(id);
        if (order is null) return NotFound();

        var vm = new AdminOrderDetailViewModel
        {
            OrderID        = order.OrderID,
            OrderCode      = order.OrderCode,
            TrackingNumber = order.TrackingNumber,
            OrderDate      = order.OrderDate,
            OrderDateLocal = dateTimeService.ConvertUtcToLocal(order.OrderDate),
            OrderEmail     = order.OrderEmail,
            CustomerName   = order.Customer?.FullName ?? (!string.IsNullOrWhiteSpace(order.ShippingRecipientName) ? order.ShippingRecipientName : "Khách vãng lai"),
            MembershipRank = order.Customer?.Membership?.MembershipName ?? "Khách vãng lai",
            ShippingName   = order.ShippingRecipientName,
            ShippingPhone  = order.ShippingPhone,
            ShippingAddress = string.IsNullOrWhiteSpace(order.ShippingFullAddress)
                ? $"{order.ShippingAddress}, {order.ShippingWard}, {order.ShippingDistrict}, {order.ShippingProvince}".Trim(',', ' ')
                : order.ShippingFullAddress,
            PaymentMethod  = order.PaymentMethod,
            PaymentStatus  = order.PaymentStatus,
            OrderStatus    = order.OrderStatus,
            TotalAmount    = order.TotalAmount,
            ShippingFee    = order.ShippingFee,
            DiscountAmount = order.DiscountAmount,
            FinalAmount    = order.FinalAmount,
            CouponCode     = order.CouponUsages.FirstOrDefault()?.Coupon?.CouponCode,
            AssignedShipperId = order.AssignedShipperId,
            AssignedShipperName = order.AssignedShipper?.UserName,
            DeliveryAttemptCount = order.DeliveryAttemptCount,
            NextDeliveryDate = order.NextDeliveryDate,
            NextDeliveryDateLocal = order.NextDeliveryDate.HasValue ? dateTimeService.ConvertUtcToLocal(order.NextDeliveryDate.Value) : null,
            DeliveryRescheduleReason = order.DeliveryRescheduleReason,
            DeliveryFailureReason = order.DeliveryFailureReason,
            DeliveryFailureReasonCode = order.DeliveryFailureReasonCode,
            Shippers = order.OrderStatus == Models.Entities.OrderStatus.ReadyToShip 
                ? await context.Accounts
                    .AsNoTracking()
                    .Where(a => a.AccountRoles.Any(ar => ar.Role.Name == "Shipper") && a.Status == Models.Entities.AccountStatus.Active)
                    .Select(a => new {
                        a.UserId,
                        a.UserName,
                        ActiveShippingCount = context.Orders.Count(o => o.AssignedShipperId == a.UserId && o.OrderStatus == Models.Entities.OrderStatus.Shipping)
                    })
                    .Select(a => new SelectListItem(
                        a.ActiveShippingCount >= SHIPPER_OVERLOAD_THRESHOLD ? $"{a.UserName} (Đang có {a.ActiveShippingCount} đơn - Quá tải)" : $"{a.UserName} (Đang giao {a.ActiveShippingCount} đơn)",
                        a.UserId.ToString()
                    ))
                    .ToListAsync() 
                : [],
            Items = order.OrderDetails.Select(d => new OrderDetailItemViewModel
            {
                ProductName = d.ProductNameSnapshot,
                SizeCode    = d.SizeCodeSnapshot,
                ColorName   = d.ColorNameSnapshot,
                SKU         = d.SKUSnapshot,
                ImageUrl    = !string.IsNullOrEmpty(d.ProductVariant?.Product?.ThumbnailUrl) 
                              ? d.ProductVariant.Product.ThumbnailUrl 
                              : (d.ProductVariant?.ProductImages?.FirstOrDefault(i => i.IsMain)?.ImageURL ?? d.ProductVariant?.ProductImages?.FirstOrDefault()?.ImageURL ?? ""),
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
                    ActionType = h.ActionType,
                    Note      = h.Note,
                    ChangedAt = h.ChangedAt,
                    ChangedAtLocal = h.ChangedAt.HasValue ? dateTimeService.ConvertUtcToLocal(h.ChangedAt.Value) : null,
                    ChangedByName = h.ChangedByAccount?.UserName
                }).ToList(),
            StatusOptions = AllStatuses
                .Select(s => new SelectListItem(s, s, s == order.OrderStatus))
                .ToList(),
            PaymentMethodOptions = new List<SelectListItem>
            {
                new("COD (Nhận hàng thanh toán)", Models.Entities.PaymentMethod.COD),
                new("Chuyển khoản ngân hàng", Models.Entities.PaymentMethod.BankTransfer),
                new("Ví MoMo", Models.Entities.PaymentMethod.MoMo),
                new("Ví ZaloPay", Models.Entities.PaymentMethod.ZaloPay),
                new("Thanh toán Online", Models.Entities.PaymentMethod.Online)
            }.Select(x => { x.Selected = x.Value == order.PaymentMethod; return x; }).ToList(),
            PaymentStatusOptions = new List<SelectListItem>
            {
                new("Chưa thanh toán", Models.Entities.PaymentStatus.Unpaid),
                new("Đã thanh toán", Models.Entities.PaymentStatus.Paid),
                new("Đã hoàn tiền", Models.Entities.PaymentStatus.Refunded),
                new("Hoàn tiền một phần", Models.Entities.PaymentStatus.PartialRefund)
            }.Select(x => { x.Selected = x.Value == order.PaymentStatus; return x; }).ToList()
        };

        return View(vm);
    }

    // ── POST /Admin/Orders/UpdateStatus ─────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    [RequirePermission("Order.Manage")]
    public async Task<IActionResult> UpdateStatus(UpdateOrderStatusInputModel input, [FromServices] ClothingStore.Data.StoreDbContext context)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return RedirectToAction(nameof(Detail), new { id = input.OrderID });
        }

        var order = await orderRepository.GetOrderByIdAsync(input.OrderID);
        if (order is null) return NotFound();

        var oldStatus   = order.OrderStatus;

        // Prevent Admin from bypassing Shipper flow
        if (oldStatus == Models.Entities.OrderStatus.Shipping && 
            (input.NewStatus == Models.Entities.OrderStatus.Delivered || input.NewStatus == Models.Entities.OrderStatus.DeliveredPendingCOD))
        {
            TempData["Error"] = "Admin không thể xác nhận giao hàng thay Shipper. Vui lòng để Shipper cập nhật trạng thái.";
            return RedirectToAction(nameof(Detail), new { id = input.OrderID });
        }
        var oldPaymentStatus = order.PaymentStatus;
        var oldPaymentMethod = order.PaymentMethod;
        var oldTrackingNumber = order.TrackingNumber;

        order.OrderStatus = input.NewStatus;

        if (!string.IsNullOrWhiteSpace(input.TrackingNumber))
            order.TrackingNumber = input.TrackingNumber.Trim();

        if (!string.IsNullOrEmpty(input.NewPaymentMethod))
            order.PaymentMethod = input.NewPaymentMethod;

        if (!string.IsNullOrEmpty(input.NewPaymentStatus))
            order.PaymentStatus = input.NewPaymentStatus;

        // Get current admin user id
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(userIdStr, out var adminUserId);

        var notes = new List<string>();
        if (oldPaymentStatus != order.PaymentStatus)
        {
            string vnLabel = order.PaymentStatus switch {
                "Unpaid" => "Chưa thanh toán",
                "Paid" => "Đã thanh toán",
                "Refunded" => "Đã hoàn tiền",
                "PartialRefund" => "Hoàn tiền một phần",
                _ => order.PaymentStatus
            };
            notes.Add($"Cập nhật trạng thái thanh toán thành '{vnLabel}'");
        }
        
        if (oldPaymentMethod != order.PaymentMethod)
        {
            string vnLabel = order.PaymentMethod switch {
                "COD" => "COD (Nhận hàng thanh toán)",
                "BankTransfer" => "Chuyển khoản ngân hàng",
                "MoMo" => "Ví MoMo",
                "ZaloPay" => "Ví ZaloPay",
                "Online" => "Thanh toán Online",
                _ => order.PaymentMethod
            };
            notes.Add($"Cập nhật hình thức thanh toán thành '{vnLabel}'");
        }

        if (oldTrackingNumber != order.TrackingNumber)
        {
            notes.Add($"Cập nhật mã vận đơn thành '{order.TrackingNumber}'");
        }

        if (!string.IsNullOrWhiteSpace(input.Note))
        {
            notes.Add(input.Note.Trim());
        }

        string? actionType = null;
        if (oldStatus == Models.Entities.OrderStatus.DeliveredPendingCOD && order.OrderStatus == Models.Entities.OrderStatus.Delivered)
        {
            actionType = "CODVerified";
            notes.Add("Admin xác nhận đối soát COD");
        }
        else if (oldStatus != order.OrderStatus)
        {
            actionType = "StatusChanged";
        }

        bool assignShipperSuccess = false;
        if (input.AssignShipperId.HasValue && input.AssignShipperId.Value > 0 && order.OrderStatus == Models.Entities.OrderStatus.ReadyToShip)
        {
            var rowsAffected = await context.Orders
                .Where(o => o.OrderID == input.OrderID && o.AssignedShipperId == null && o.OrderStatus == Models.Entities.OrderStatus.ReadyToShip)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(o => o.AssignedShipperId, input.AssignShipperId)
                    .SetProperty(o => o.AssignedAt, DateTime.UtcNow));

            if (rowsAffected == 0)
            {
                TempData["Error"] = "Đơn hàng đã được phân công hoặc trạng thái đã thay đổi (có thể do thao tác đồng thời).";
                return RedirectToAction(nameof(Detail), new { id = input.OrderID });
            }

            order.AssignedShipperId = input.AssignShipperId;
            order.AssignedAt = DateTime.UtcNow;
            assignShipperSuccess = true;

            var shipper = await context.Accounts.FindAsync(input.AssignShipperId.Value);
            notes.Add($"Đã giao đơn cho Shipper '{shipper?.UserName}'");

            order.StatusHistory.Add(new Models.Entities.OrderStatusHistory
            {
                OldStatus = oldStatus,
                NewStatus = order.OrderStatus,
                ActionType = "AssignShipper",
                Note = $"Admin giao đơn cho shipper {shipper?.UserName}",
                ChangedAt = DateTime.UtcNow,
                ChangedBy = adminUserId > 0 ? adminUserId : null
            });
        }

        bool isStatusChanged = oldStatus != order.OrderStatus;
        if (isStatusChanged || (notes.Count > 0 && !assignShipperSuccess))
        {
            order.StatusHistory.Add(new Models.Entities.OrderStatusHistory
            {
                OldStatus = oldStatus,
                NewStatus = order.OrderStatus,
                ActionType = actionType,
                Note      = notes.Count > 0 ? string.Join("\n", notes) : null,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = adminUserId > 0 ? adminUserId : null
            });
        }

        await unitOfWork.SaveChangesAsync();

        TempData["Success"] = $"Đã cập nhật trạng thái đơn hàng thành '{input.NewStatus}'.";
        return RedirectToAction(nameof(Detail), new { id = input.OrderID });
    }
}
