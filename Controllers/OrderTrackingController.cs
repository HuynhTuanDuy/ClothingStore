using ClothingStore.Models.Entities;
using ClothingStore.Models.ViewModels;
using ClothingStore.Repositories;
using ClothingStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace ClothingStore.Controllers;

public class OrderTrackingController(
    IOrderRepository orderRepository,
    ICartService cartService,
    IProductRepository productRepository,
    IMemoryCache memoryCache,
    IDateTimeService dateTimeService,
    ILogger<OrderTrackingController> logger) : Controller
{
    private const int MaxAttempts = 5;
    private const int LockoutMinutes = 15;

    [HttpGet]
    [Route("order-tracking")]
    public IActionResult Index([FromQuery] string? code, [FromQuery] string? phone)
    {
        var model = new GuestOrderTrackingForm { OrderCode = code ?? "", PhoneNumber = phone ?? "" };
        return View(model);
    }

    [HttpPost]
    [Route("order-tracking")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(GuestOrderTrackingForm form)
    {
        if (!ModelState.IsValid)
            return View(form);

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var cacheKey = $"OrderTrack_{clientIp}";

        if (memoryCache.TryGetValue(cacheKey, out int attempts) && attempts >= MaxAttempts)
        {
            logger.LogWarning("Rate limit exceeded for IP: {ClientIp} trying to track order: {OrderCode}", clientIp, form.OrderCode);
            ModelState.AddModelError("", "Bạn đã tra cứu sai quá nhiều lần. Vui lòng thử lại sau 15 phút.");
            return View(form);
        }

        try
        {
            var order = await orderRepository.GetOrderForGuestTrackingAsync(form.OrderCode.Trim(), form.PhoneNumber.Trim());

            if (order == null)
            {
                attempts++;
                memoryCache.Set(cacheKey, attempts, TimeSpan.FromMinutes(LockoutMinutes));
                logger.LogInformation("Failed tracking attempt {Attempt} for IP: {ClientIp}, Order: {OrderCode}", attempts, clientIp, form.OrderCode);
                ModelState.AddModelError("", "Không tìm thấy đơn hàng. Vui lòng kiểm tra lại Mã đơn hàng và Số điện thoại.");
                return View(form);
            }

            // Reset attempts on success
            memoryCache.Remove(cacheKey);
            logger.LogInformation("Order {OrderCode} tracked successfully from IP {ClientIp} at {Time}", order.OrderCode, clientIp, DateTime.UtcNow);

            var result = new GuestOrderTrackingResult
            {
                OrderCode = order.OrderCode,
                OrderDate = dateTimeService.ConvertUtcToLocal(order.OrderDate),
                OrderStatus = order.OrderStatus,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,
                Subtotal = order.TotalAmount + order.DiscountAmount - order.ShippingFee,
                ShippingFee = order.ShippingFee,
                DiscountAmount = order.DiscountAmount,
                TotalAmount = order.TotalAmount,
                MaskedRecipientName = GuestOrderTrackingResult.MaskName(order.ShippingRecipientName),
                MaskedPhone = GuestOrderTrackingResult.MaskPhone(order.ShippingPhone),
                MaskedAddress = GuestOrderTrackingResult.MaskAddress(order.ShippingAddress, order.ShippingWard, order.ShippingDistrict, order.ShippingProvince),
                CanReorder = order.OrderStatus is OrderStatus.Delivered or OrderStatus.Shipping,
                Items = order.OrderDetails.Select(od => new GuestOrderTrackingItem
                {
                    VariantID = od.VariantID,
                    ProductName = od.ProductNameSnapshot,
                    ColorName = od.ColorNameSnapshot,
                    SizeCode = od.SizeCodeSnapshot,
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                    ImageUrl = od.ProductVariant?.ProductImages?.FirstOrDefault(img => img.IsMain)?.ImageURL 
                                ?? od.ProductVariant?.ProductImages?.FirstOrDefault()?.ImageURL 
                                ?? "/images/placeholder.jpg",
                    ProductSlug = od.ProductVariant?.Product?.Slug ?? ""
                }).ToList(),
                Histories = order.StatusHistory.Select(sh => new GuestOrderTrackingHistory
                {
                    Status = sh.NewStatus,
                    ChangedAt = dateTimeService.ConvertUtcToLocal(sh.ChangedAt ?? DateTime.UtcNow),
                    Note = sh.Note
                }).ToList()
            };

            ViewBag.Result = result;
            return View(form);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while tracking order {OrderCode} for IP {ClientIp}", form.OrderCode, clientIp);
            ModelState.AddModelError("", "Đã xảy ra lỗi trong quá trình tra cứu đơn hàng. Vui lòng thử lại sau.");
            return View(form);
        }
    }

    [HttpPost]
    [Route("order-tracking/reorder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder(string orderCode, string phone)
    {
        try
        {
            var order = await orderRepository.GetOrderForGuestTrackingAsync(orderCode, phone);
            if (order == null)
                return NotFound();

            if (order.OrderStatus is not (OrderStatus.Delivered or OrderStatus.Shipping))
            {
                logger.LogWarning("Attempted to reorder an invalid order status. Order: {OrderCode}, Status: {Status}", orderCode, order.OrderStatus);
                TempData["Error"] = "Đơn hàng này không thể mua lại.";
                return RedirectToAction("Index", new { code = orderCode });
            }

            var missingItems = new List<string>();
            foreach (var detail in order.OrderDetails)
            {
                var variant = await productRepository.GetVariantAsync(detail.VariantID);
                if (variant == null || !variant.IsActive || !variant.Product.IsActive)
                {
                    missingItems.Add(detail.ProductNameSnapshot);
                    continue;
                }

                int qtyToAdd = Math.Min(detail.Quantity, variant.StockQuantity);
                if (qtyToAdd > 0)
                {
                    await cartService.AddItemAsync(new AddToCartInputModel { VariantID = detail.VariantID, Quantity = qtyToAdd });
                }
                else
                {
                    missingItems.Add(detail.ProductNameSnapshot);
                }
            }

            if (missingItems.Any())
            {
                TempData["Warning"] = $"Một số sản phẩm không được thêm vì đã hết hàng hoặc ngừng kinh doanh: {string.Join(", ", missingItems)}";
                logger.LogInformation("Reorder partial success for Order: {OrderCode}. Missing items: {Items}", orderCode, string.Join(", ", missingItems));
            }
            else
            {
                TempData["Success"] = "Đã thêm lại các sản phẩm vào giỏ hàng.";
                logger.LogInformation("Reorder successful for Order: {OrderCode}", orderCode);
            }

            return RedirectToAction("Index", "Cart");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while reordering Order {OrderCode}", orderCode);
            TempData["Error"] = "Đã xảy ra lỗi trong quá trình mua lại đơn hàng. Vui lòng thử lại sau.";
            return RedirectToAction("Index", new { code = orderCode });
        }
    }
}
