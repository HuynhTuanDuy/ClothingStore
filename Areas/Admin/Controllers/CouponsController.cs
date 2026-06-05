using ClothingStore.Models.Entities;
using ClothingStore.Models.ViewModels;
using ClothingStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClothingStore.Areas.Admin.Controllers;

[Area("Admin")]
// [Authorize(Roles = "Admin")]
public class CouponsController(ICouponService couponService) : Controller
{
    // ── GET /Admin/Coupons ──────────────────────────────────────
    public async Task<IActionResult> Index([FromQuery] CouponFilter filter)
    {
        var (coupons, stats) = await couponService.GetCouponsFilteredAsync(filter);
        ViewBag.Stats = stats;
        ViewBag.CurrentFilter = filter;
        return View(coupons);
    }

    // ── GET /Admin/Coupons/Create ───────────────────────────────
    public IActionResult Create()
    {
        return View(new CouponEditViewModel
        {
            ValidFrom = DateTime.Today,
            ValidTo   = DateTime.Today.AddMonths(1),
            IsActive  = true
        });
    }

    // ── POST /Admin/Coupons/Create ──────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CouponEditViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        if (model.ValidTo <= model.ValidFrom)
        {
            ModelState.AddModelError(nameof(model.ValidTo), "Ngày kết thúc phải sau ngày bắt đầu.");
            return View(model);
        }

        var coupon = ToCoupon(model);
        var saved  = await couponService.SaveCouponAsync(coupon);
        if (!saved)
        {
            ModelState.AddModelError(string.Empty, "Không thể lưu mã giảm giá.");
            return View(model);
        }

        TempData["Success"] = "Tạo mã giảm giá thành công.";
        return RedirectToAction(nameof(Index));
    }

    // ── GET /Admin/Coupons/Edit/{id} ────────────────────────────
    public async Task<IActionResult> Edit(int id)
    {
        var coupon = await couponService.GetCouponByIdAsync(id);
        if (coupon is null) return NotFound();
        return View(ToViewModel(coupon));
    }

    // ── POST /Admin/Coupons/Edit ────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CouponEditViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        if (model.ValidTo <= model.ValidFrom)
        {
            ModelState.AddModelError(nameof(model.ValidTo), "Ngày kết thúc phải sau ngày bắt đầu.");
            return View(model);
        }

        var coupon = ToCoupon(model);
        var saved  = await couponService.SaveCouponAsync(coupon);
        if (!saved)
        {
            ModelState.AddModelError(string.Empty, "Mã giảm giá không tồn tại.");
            return View(model);
        }

        TempData["Success"] = "Cập nhật mã giảm giá thành công.";
        return RedirectToAction(nameof(Index));
    }

    // ── POST /Admin/Coupons/Toggle ──────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        await couponService.ToggleCouponAsync(id);
        TempData["Success"] = "Đã thay đổi trạng thái mã giảm giá.";
        return RedirectToAction(nameof(Index));
    }

    private static Coupon ToCoupon(CouponEditViewModel m) => new()
    {
        CouponID      = m.CouponID,
        CouponCode    = m.CouponCode,
        DiscountType  = m.DiscountType,
        DiscountValue = m.DiscountValue,
        MinOrderValue = m.MinOrderValue,
        UsageLimit    = m.UsageLimit,
        ValidFrom     = m.ValidFrom.ToUniversalTime(),
        ValidTo       = m.ValidTo.ToUniversalTime(),
        IsActive      = m.IsActive
    };

    private static CouponEditViewModel ToViewModel(Coupon c) => new()
    {
        CouponID      = c.CouponID,
        CouponCode    = c.CouponCode,
        DiscountType  = c.DiscountType,
        DiscountValue = c.DiscountValue,
        MinOrderValue = c.MinOrderValue,
        UsageLimit    = c.UsageLimit,
        ValidFrom     = c.ValidFrom.ToLocalTime(),
        ValidTo       = c.ValidTo.ToLocalTime(),
        IsActive      = c.IsActive,
        UsedCount     = c.UsedCount,
        UsageHistory  = c.CouponUsages.OrderByDescending(u => u.UsedAt).Select(u => new CouponUsageHistoryViewModel
        {
            UsageID = u.UsageID,
            UsedAt = u.UsedAt.ToLocalTime(),
            CustomerId = u.CustomerId,
            CustomerName = u.Customer?.FullName ?? (u.Order != null ? u.Order.ShippingRecipientName : "Guest"),
            OrderCode = u.Order?.OrderCode ?? string.Empty,
            OrderStatus = u.Order?.OrderStatus ?? string.Empty,
            DiscountAmount = u.Order?.DiscountAmount ?? 0m
        }).ToList()
    };
}
