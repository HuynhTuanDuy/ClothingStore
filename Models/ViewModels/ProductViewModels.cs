using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClothingStore.Models.ViewModels;

// ─────────────────────────────────────────────────────────────
// PRODUCT LIST
// ─────────────────────────────────────────────────────────────
public class ProductListViewModel
{
    public string? Search { get; set; }
    public int? CategoryID { get; set; }
    public string? Gender { get; set; }
    public int? ColorId { get; set; }
    public int? SizeId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public string? SortBy { get; set; }
    
    public List<SelectListItem> SortOptions { get; } = new List<SelectListItem>
    {
        new SelectListItem { Value = "newest", Text = "Mới nhất" },
        new SelectListItem { Value = "bestselling", Text = "Bán chạy nhất" },
        new SelectListItem { Value = "priceasc", Text = "Giá: Thấp đến cao" },
        new SelectListItem { Value = "pricedesc", Text = "Giá: Cao đến thấp" },
        new SelectListItem { Value = "nameasc", Text = "Tên: A-Z" },
        new SelectListItem { Value = "namedesc", Text = "Tên: Z-A" }
    };

    public List<CategoryFilterViewModel> Categories { get; set; } = [];
    public List<SizeFilterViewModel> Sizes { get; set; } = [];
    public List<ColorFilterViewModel> Colors { get; set; } = [];
    public List<ProductCardViewModel> Products { get; set; } = [];
}

public class CategoryFilterViewModel
{
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}

public class SizeFilterViewModel
{
    public int SizeID { get; set; }
    public string SizeCode { get; set; } = string.Empty;
}

public class ColorFilterViewModel
{
    public int ColorID { get; set; }
    public string ColorName { get; set; } = string.Empty;
    public string HexCode { get; set; } = "#000000";
}

// ─────────────────────────────────────────────────────────────
// PRODUCT CARD
// ─────────────────────────────────────────────────────────────
public class ProductCardViewModel
{
    public int ProductID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    /// <summary>Original price before discount (for strikethrough).</summary>
    public decimal? OriginalMinPrice { get; set; }
    public bool HasDiscount { get; set; }
    public int DiscountPercent { get; set; }
    public bool HasStock { get; set; }
    public bool IsBestSeller { get; set; }
    public List<ColorFilterViewModel> Colors { get; set; } = [];
}

// ─────────────────────────────────────────────────────────────
// PRODUCT DETAIL
// ─────────────────────────────────────────────────────────────
public class ProductDetailViewModel
{
    public int ProductID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? Description { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string FitType { get; set; } = string.Empty;
    public string CareInstructions { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int CategoryID { get; set; }
    public List<string> ImageUrls { get; set; } = [];
    public List<ProductVariantViewModel> Variants { get; set; } = [];
    public List<ReviewViewModel> Reviews { get; set; } = [];
    public List<ProductCardViewModel> RelatedProducts { get; set; } = [];
    public List<ProductCardViewModel> BestSellingProducts { get; set; } = [];
    public List<ProductCardViewModel> InStockProducts { get; set; } = [];

    // Helpers for the view
    public decimal MinEffectivePrice => Variants.Count > 0 ? Variants.Min(v => v.EffectivePrice) : 0;
    public bool HasDiscount => Variants.Any(v => v.HasDiscount);
}

public class ProductVariantViewModel
{
    public int VariantID { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string SizeCode { get; set; } = string.Empty;
    public string ColorName { get; set; } = string.Empty;
    public string HexCode { get; set; } = "#000000";
    public decimal SellingPrice { get; set; }
    public decimal EffectivePrice { get; set; }
    public bool HasDiscount { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
    /// <summary>Images for this specific variant — used by JS to switch gallery on color change.</summary>
    public List<string> ImageUrls { get; set; } = [];
}

public class ReviewViewModel
{
    public int ReviewID { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime ReviewDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
}
