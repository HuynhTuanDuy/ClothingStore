using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClothingStore.Models.ViewModels;

// ─────────────────────────────────────────────────────────────
// ADMIN — PRODUCT
// ─────────────────────────────────────────────────────────────
public class AdminProductListItemViewModel
{
    public int ProductID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int VariantCount { get; set; }
    public int TotalStock { get; set; }
    public int TotalSold { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class AdminProductFilter
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public string? Status { get; set; } // active, inactive
    public string? StockStatus { get; set; } // instock, outofstock, lowstock
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? HasDiscount { get; set; } // yes, no
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? SortBy { get; set; } // name, price, stock, sold, category, status, created, updated
    public bool SortDesc { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class ProductDashboardStatsViewModel
{
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int InactiveProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public int LowStockProducts { get; set; }
}

public class AdminProductPageViewModel
{
    public List<AdminProductListItemViewModel> Products { get; set; } = [];
    public AdminProductFilter Filter { get; set; } = new();
    public ProductDashboardStatsViewModel Stats { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages => Filter.PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / Filter.PageSize) : 0;
}

public class ProductEditViewModel
{
    public int ProductID { get; set; }

    [Required(ErrorMessage = "Tên sản phẩm là bắt buộc."), StringLength(200)]
    public string ProductName { get; set; } = string.Empty;

    [StringLength(250)]
    public string Slug { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ThumbnailUrl { get; set; }

    public string? Description { get; set; }

    [Required(ErrorMessage = "Giới tính là bắt buộc."), StringLength(20)]
    public string Gender { get; set; } = string.Empty;

    [Required(ErrorMessage = "Chất liệu là bắt buộc."), StringLength(100)]
    public string Material { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kiểu dáng là bắt buộc."), StringLength(50)]
    public string FitType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Hướng dẫn bảo quản là bắt buộc."), StringLength(500)]
    public string CareInstructions { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn danh mục.")]
    public int CategoryID { get; set; }

    public int? ProgramID { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsBestSeller { get; set; } = false;

    // For manual related products
    public List<int>? SelectedRelatedProductIds { get; set; } = [];

    // Dropdown lists
    public List<SelectListItem> Categories { get; set; } = [];
    public List<SelectListItem> DiscountPrograms { get; set; } = [];
    public List<SelectListItem> AvailableProducts { get; set; } = [];
    
    // For Attributes page
    public List<ColorFilterViewModel> AvailableColors { get; set; } = [];
    public List<SizeFilterViewModel> AvailableSizes { get; set; } = [];
}

public class VariantEditViewModel
{
    public int VariantID { get; set; }
    public int ProductID { get; set; }
    public string ProductName { get; set; } = string.Empty;

    [Required(ErrorMessage = "SKU là bắt buộc."), StringLength(50)]
    public string SKU { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải >= 0.")]
    public decimal SellingPrice { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Tồn kho phải >= 0.")]
    public int StockQuantity { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn kích thước.")]
    public int SizeID { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn màu sắc.")]
    public int ColorID { get; set; }

    public bool IsActive { get; set; } = true;
    public IReadOnlyList<SelectListItem> Sizes { get; set; } = [];
    public IReadOnlyList<SelectListItem> Colors { get; set; } = [];
}

public class AdminVariantListViewModel
{
    public int ProductID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public IReadOnlyList<ProductVariantViewModel> Variants { get; set; } = [];
}

public class ProductImageUploadViewModel
{
    public int ProductID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn biến thể.")]
    public int VariantID { get; set; }
    [Range(1, int.MaxValue)]
    public int DisplayOrder { get; set; } = 1;
    public bool IsMain { get; set; }
    public List<IFormFile> ImageFiles { get; set; } = [];
    public IReadOnlyList<SelectListItem> Variants { get; set; } = [];
}

public class ProductImageListItemViewModel
{
    public int ImageID { get; set; }
    public int VariantID { get; set; }
    public string VariantName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string ImageURL { get; set; } = string.Empty;
    public bool IsMain { get; set; }
}

public class AdminProductImagesViewModel
{
    public ProductImageUploadViewModel Upload { get; set; } = new();
    public IReadOnlyList<ProductImageListItemViewModel> Images { get; set; } = [];
    public string? ProductThumbnailUrl { get; set; }
}

// ─────────────────────────────────────────────────────────────
// ADMIN — DASHBOARD
// ─────────────────────────────────────────────────────────────
public class DashboardViewModel
{
    public int Year { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int LowStockVariants { get; set; }
    public int TotalCustomers { get; set; }
    
    // New dynamic stats
    public int ShippingOrders { get; set; }
    public double CompletionRate { get; set; }
    public int TotalProductsSold { get; set; }

    public IReadOnlyList<string> RevenueLabels { get; set; } = [];
    public IReadOnlyList<decimal> RevenueValues { get; set; } = [];
    public IReadOnlyList<string> CategorySalesLabels { get; set; } = [];
    public IReadOnlyList<int> CategorySalesValues { get; set; } = [];
    public IReadOnlyList<TopProductViewModel> TopProducts { get; set; } = [];
    public IReadOnlyList<RecentOrderViewModel> RecentOrders { get; set; } = [];
    public IReadOnlyList<LowStockItemViewModel> LowStockItems { get; set; } = [];
    public IReadOnlyList<LowStockProductViewModel> LowStockProducts { get; set; } = [];
}

public class TopProductViewModel
{
    public string ProductName { get; set; } = string.Empty;
    public int TotalSold { get; set; }
    public decimal Revenue { get; set; }
}

public class RecentOrderViewModel
{
    public string OrderCode { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string OrderEmail { get; set; } = string.Empty;
    public decimal FinalAmount { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
}

public class LowStockItemViewModel
{
    public string SKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string SizeCode { get; set; } = string.Empty;
    public string ColorName { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
}

public class LowStockProductViewModel
{
    public int ProductID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int TotalStock { get; set; }
    public List<LowStockVariantViewModel> Variants { get; set; } = [];
}

public class LowStockVariantViewModel
{
    public string SKU { get; set; } = string.Empty;
    public string SizeCode { get; set; } = string.Empty;
    public string ColorName { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
}

// ─────────────────────────────────────────────────────────────
// ADMIN — ORDERS
// ─────────────────────────────────────────────────────────────
public class AdminOrderListViewModel
{
    public List<AdminOrderSummaryViewModel> Orders { get; set; } = [];
    public string? StatusFilter { get; set; }
    public int? FilterDay { get; set; }
    public int? FilterMonth { get; set; }
    public int? FilterYear { get; set; }
    public int Page { get; set; }
    public int TotalCount { get; set; }
    public int PageSize { get; set; } = 20;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public int ShippingOrders { get; set; }
    public double CompletionRate { get; set; }
    public int TotalProductsSold { get; set; }
}

public class AdminOrderSummaryViewModel
{
    public int OrderID { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string OrderEmail { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal FinalAmount { get; set; }
    public int ItemCount { get; set; }
    public string MembershipRank { get; set; } = string.Empty;
}

public class AdminOrderDetailViewModel
{
    public int OrderID { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string? TrackingNumber { get; set; }
    public DateTime OrderDate { get; set; }
    public string OrderEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string MembershipRank { get; set; } = string.Empty;
    public string ShippingName { get; set; } = string.Empty;
    public string ShippingPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public string? CouponCode { get; set; }
    public List<OrderDetailItemViewModel> Items { get; set; } = [];
    public List<OrderStatusHistoryViewModel> StatusHistory { get; set; } = [];
    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = [];
    public IReadOnlyList<SelectListItem> PaymentMethodOptions { get; set; } = [];
    public IReadOnlyList<SelectListItem> PaymentStatusOptions { get; set; } = [];
}

public class UpdateOrderStatusInputModel
{
    public int OrderID { get; set; }
    [Required] public string NewStatus { get; set; } = string.Empty;
    public string? NewPaymentMethod { get; set; }
    public string? NewPaymentStatus { get; set; }
    public string? Note { get; set; }
    public string? TrackingNumber { get; set; }
}

// ─────────────────────────────────────────────────────────────
// ADMIN — COUPONS
// ─────────────────────────────────────────────────────────────
public class CouponEditViewModel
{
    public int CouponID { get; set; }

    [Required, StringLength(50)]
    public string CouponCode { get; set; } = string.Empty;

    [Required]
    public string DiscountType { get; set; } = "Percentage";

    [Range(0.01, double.MaxValue, ErrorMessage = "Giá trị giảm phải > 0.")]
    public decimal DiscountValue { get; set; }

    [Range(0, double.MaxValue)]
    public decimal MinOrderValue { get; set; }

    public int? UsageLimit { get; set; }

    [Required]
    public DateTime ValidFrom { get; set; } = DateTime.Today;

    [Required]
    public DateTime ValidTo { get; set; } = DateTime.Today.AddMonths(1);

    public bool IsActive { get; set; } = true;
}

// ─────────────────────────────────────────────────────────────
// AUTH
// ─────────────────────────────────────────────────────────────
public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập hoặc email.")]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3-50 ký tự.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email là bắt buộc.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu ít nhất 6 ký tự.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ tên là bắt buộc.")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string Phone { get; set; } = string.Empty;
}
