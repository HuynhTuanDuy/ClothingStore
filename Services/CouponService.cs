using ClothingStore.Data;
using ClothingStore.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClothingStore.Services;

public record CouponValidationResult(
    bool IsValid,
    string? ErrorMessage,
    int CouponId = 0,
    decimal DiscountAmount = 0m,
    string CouponCode = ""
);

public class CouponFilter
{
    public string? SearchKeyword { get; set; }
    public string? Status { get; set; } // "Active", "Inactive", "Expired", "Valid", "UsedUp"
}

public class CouponDashboardStats
{
    public int TotalCoupons { get; set; }
    public int ActiveCoupons { get; set; }
    public int ExpiredCoupons { get; set; }
    public int UsedUpCoupons { get; set; }
}

public interface ICouponService
{
    Task<CouponValidationResult> ValidateAsync(string couponCode, decimal orderSubTotal, int? customerId = null);
    Task<(List<Coupon> Coupons, CouponDashboardStats Stats)> GetCouponsFilteredAsync(CouponFilter filter);
    Task<Coupon?> GetCouponByIdAsync(int couponId);
    Task<bool> SaveCouponAsync(Coupon coupon);
    Task<bool> ToggleCouponAsync(int couponId);
}

public class CouponService(StoreDbContext dbContext) : ICouponService
{
    /// <summary>
    /// [HIGH-01 FIX] Validate coupon and calculate discount.
    /// Uses UsedCount for performance (DB-04).
    /// </summary>
    public async Task<CouponValidationResult> ValidateAsync(
        string couponCode, decimal orderSubTotal, int? customerId = null)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
            return new CouponValidationResult(false, "Vui lòng nhập mã giảm giá.");

        var coupon = await dbContext.Coupons
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CouponCode == couponCode.Trim().ToUpperInvariant());

        if (coupon is null)
            return new CouponValidationResult(false, "Mã giảm giá không tồn tại.");

        if (!coupon.IsActive)
            return new CouponValidationResult(false, "Mã giảm giá đã bị vô hiệu hóa.");

        if (DateTime.UtcNow < coupon.ValidFrom)
            return new CouponValidationResult(false, $"Mã giảm giá có hiệu lực từ {coupon.ValidFrom:dd/MM/yyyy}.");

        if (DateTime.UtcNow > coupon.ValidTo)
            return new CouponValidationResult(false, "Mã giảm giá đã hết hạn.");

        if (orderSubTotal < coupon.MinOrderValue)
            return new CouponValidationResult(false,
                $"Đơn hàng cần tối thiểu {coupon.MinOrderValue:N0}đ để dùng mã này.");

        if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
            return new CouponValidationResult(false, "Mã giảm giá đã hết lượt sử dụng.");

        var discountAmount = coupon.CalculateDiscount(orderSubTotal);
        return new CouponValidationResult(true, null, coupon.CouponID, discountAmount, coupon.CouponCode);
    }

    public async Task<(List<Coupon> Coupons, CouponDashboardStats Stats)> GetCouponsFilteredAsync(CouponFilter filter)
    {
        var query = dbContext.Coupons.AsNoTracking().AsQueryable();

        // Calculate stats before applying search filters (but we can do it on the whole set)
        var allCoupons = await query.ToListAsync();
        var now = DateTime.UtcNow;

        var stats = new CouponDashboardStats
        {
            TotalCoupons = allCoupons.Count,
            ActiveCoupons = allCoupons.Count(c => c.IsActive),
            ExpiredCoupons = allCoupons.Count(c => c.ValidTo < now),
            UsedUpCoupons = allCoupons.Count(c => c.UsageLimit.HasValue && c.UsedCount >= c.UsageLimit.Value)
        };

        // Filter
        if (!string.IsNullOrWhiteSpace(filter.SearchKeyword))
        {
            var kw = filter.SearchKeyword.Trim().ToLower();
            query = query.Where(c => c.CouponCode.ToLower().Contains(kw));
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            switch (filter.Status)
            {
                case "Active":
                    query = query.Where(c => c.IsActive);
                    break;
                case "Inactive":
                    query = query.Where(c => !c.IsActive);
                    break;
                case "Expired":
                    query = query.Where(c => c.ValidTo < now);
                    break;
                case "Valid":
                    query = query.Where(c => c.IsActive && c.ValidFrom <= now && c.ValidTo >= now && (c.UsageLimit == null || c.UsedCount < c.UsageLimit));
                    break;
                case "UsedUp":
                    query = query.Where(c => c.UsageLimit != null && c.UsedCount >= c.UsageLimit);
                    break;
            }
        }

        var coupons = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
        return (coupons, stats);
    }

    public Task<Coupon?> GetCouponByIdAsync(int couponId)
    {
        return dbContext.Coupons
            .Include(c => c.CouponUsages)
                .ThenInclude(u => u.Customer)
            .Include(c => c.CouponUsages)
                .ThenInclude(u => u.Order)
            .FirstOrDefaultAsync(c => c.CouponID == couponId);
    }

    public async Task<bool> SaveCouponAsync(Coupon coupon)
    {
        if (coupon.ValidTo <= coupon.ValidFrom) return false;

        if (coupon.CouponID == 0)
        {
            coupon.CouponCode = coupon.CouponCode.Trim().ToUpperInvariant();
            coupon.CreatedAt = DateTime.UtcNow;
            coupon.UpdatedAt = DateTime.UtcNow;
            await dbContext.Coupons.AddAsync(coupon);
        }
        else
        {
            var existing = await dbContext.Coupons.FindAsync(coupon.CouponID);
            if (existing is null) return false;
            existing.CouponCode    = coupon.CouponCode.Trim().ToUpperInvariant();
            existing.DiscountType  = coupon.DiscountType;
            existing.DiscountValue = coupon.DiscountValue;
            existing.MinOrderValue = coupon.MinOrderValue;
            existing.UsageLimit    = coupon.UsageLimit;
            existing.ValidFrom     = coupon.ValidFrom;
            existing.ValidTo       = coupon.ValidTo;
            existing.IsActive      = coupon.IsActive;
            existing.UpdatedAt     = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleCouponAsync(int couponId)
    {
        var coupon = await dbContext.Coupons.FindAsync(couponId);
        if (coupon is null) return false;
        coupon.IsActive = !coupon.IsActive;
        coupon.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        return true;
    }
}
