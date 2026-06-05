using System.ComponentModel.DataAnnotations;
using ClothingStore.Models.Entities;

namespace ClothingStore.Models.ViewModels;

public class GuestOrderTrackingForm
{
    [Required(ErrorMessage = "Vui lòng nhập Mã đơn hàng")]
    public string OrderCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập Số điện thoại mua hàng")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string PhoneNumber { get; set; } = string.Empty;
}

public class GuestOrderTrackingResult
{
    public string OrderCode { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;

    // Masked Data
    public string MaskedRecipientName { get; set; } = string.Empty;
    public string MaskedPhone { get; set; } = string.Empty;
    public string MaskedAddress { get; set; } = string.Empty;

    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public bool CanReorder { get; set; }

    public List<GuestOrderTrackingItem> Items { get; set; } = new();
    public List<GuestOrderTrackingHistory> Histories { get; set; } = new();

    public static string MaskName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return string.Empty;
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= 1)
        {
            return fullName.Length > 2 ? $"{fullName.Substring(0, 1)}***{fullName.Substring(fullName.Length - 1)}" : fullName;
        }
        
        var masked = parts.Select((p, i) => 
            (i == 0 || i == parts.Length - 1) ? p : $"{p[0]}***"
        ).ToArray();
        return string.Join(" ", masked);
    }

    public static string MaskPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return string.Empty;
        var clean = phone.Trim();
        if (clean.Length < 4) return clean;
        return new string('*', clean.Length - 3) + clean.Substring(clean.Length - 3);
    }

    public static string MaskAddress(string address, string ward, string district, string province)
    {
        var addressParts = address?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        string maskedStreet = addressParts.Length > 0 ? "***" : "";
        
        var locationParts = new[] { ward, district, province }.Where(x => !string.IsNullOrWhiteSpace(x));
        var locationStr = string.Join(", ", locationParts);
        
        return string.IsNullOrWhiteSpace(maskedStreet) ? locationStr : $"{maskedStreet}, {locationStr}";
    }
}

public class GuestOrderTrackingItem
{
    public int VariantID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ColorName { get; set; } = string.Empty;
    public string SizeCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
}

public class GuestOrderTrackingHistory
{
    public string Status { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? Note { get; set; }
}
