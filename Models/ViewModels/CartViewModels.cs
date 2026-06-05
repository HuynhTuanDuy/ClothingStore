namespace ClothingStore.Models.ViewModels;

// ─────────────────────────────────────────────────────────────
// CART
// ─────────────────────────────────────────────────────────────
public class CartViewModel
{
    public int CartID { get; set; }
    public List<CartItemViewModel> Items { get; set; } = [];
    public bool IsEmpty => Items.Count == 0;
    public decimal SubTotal => Items.Sum(x => x.LineTotal);
    public int TotalItems => Items.Sum(x => x.Quantity);
    public List<ProductCardViewModel> RecommendedProducts { get; set; } = [];
}

public class CartItemViewModel
{
    public int CartItemID { get; set; }
    public int VariantID { get; set; }
    public int ProductID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string SizeCode { get; set; } = string.Empty;
    public string ColorName { get; set; } = string.Empty;
    public string HexCode { get; set; } = "#000000";
    public string SKU { get; set; } = string.Empty;
    /// <summary>Original price before any discount.</summary>
    public decimal OriginalPrice { get; set; }
    /// <summary>Effective price after discount program.</summary>
    public decimal UnitPrice { get; set; }
    public bool HasDiscount { get; set; }
    public int Quantity { get; set; }
    public int StockQuantity { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
    public decimal Savings => (OriginalPrice - UnitPrice) * Quantity;
}

public class AddToCartInputModel
{
    public int VariantID { get; set; }
    public int Quantity { get; set; } = 1;
}
