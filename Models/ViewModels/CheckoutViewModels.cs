using System.ComponentModel.DataAnnotations;

namespace ClothingStore.Models.ViewModels;

// ─────────────────────────────────────────────────────────────
// CHECKOUT
// ─────────────────────────────────────────────────────────────
public class CheckoutViewModel
{
    public CheckoutInputModel Input { get; set; } = new();
    public CartViewModel Cart { get; set; } = new();
    public decimal SubTotal => Cart.SubTotal;
    public decimal ShippingFee => SubTotal >= 500_000m ? 0m : 30_000m;
    public decimal DiscountAmount { get; set; }
    public string? AppliedCouponCode { get; set; }
    public decimal VAT => Math.Max(0m, (SubTotal - DiscountAmount) * 0.1m);
    public decimal FinalAmount => SubTotal + ShippingFee - DiscountAmount + VAT;
    
    // New properties
    public bool IsAuthenticated { get; set; }
    public List<ClothingStore.Models.Entities.ShippingAddress> SavedAddresses { get; set; } = new();
}

public class CheckoutInputModel
{
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string OrderEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên người nhận.")]
    [StringLength(100)]
    public string ShippingRecipientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string ShippingPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
    [StringLength(255)]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập phường/xã.")]
    [StringLength(50)]
    public string ShippingWard { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập quận/huyện.")]
    [StringLength(50)]
    public string ShippingDistrict { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tỉnh/thành phố.")]
    [StringLength(50)]
    public string ShippingProvince { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
    public string PaymentMethod { get; set; } = "COD";

    /// <summary>Optional coupon code entered by customer.</summary>
    public string? CouponCode { get; set; }
}

// ─────────────────────────────────────────────────────────────
// PLACE ORDER RESULT
// ─────────────────────────────────────────────────────────────
public class PlaceOrderResult
{
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }
    public string? OrderCode { get; init; }

    public static PlaceOrderResult Success(string orderCode) =>
        new() { Succeeded = true, OrderCode = orderCode };

    public static PlaceOrderResult Failure(string error) =>
        new() { Succeeded = false, ErrorMessage = error };
}

// ─────────────────────────────────────────────────────────────
// PAYMENT PENDING (Bank / MoMo / ZaloPay)
// ─────────────────────────────────────────────────────────────
public class PaymentPendingViewModel
{
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal FinalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;

    // Account info determined per payment method
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;

    /// <summary>Pre-filled transfer content: DH_[OrderCode]_[CustomerName]</summary>
    public string TransferContent => $"DH_{OrderCode}_{CustomerName}";

    /// <summary>QR URL using VietQR (bank) or simple QR for MoMo/ZaloPay</summary>
    public string QrImageUrl { get; set; } = string.Empty;
}

// ─────────────────────────────────────────────────────────────
// COUPON
// ─────────────────────────────────────────────────────────────
public record CouponApplyResult(
    bool IsValid,
    string? ErrorMessage,
    decimal DiscountAmount,
    string CouponCode
);

// ─────────────────────────────────────────────────────────────
// ORDER SUCCESS
// ─────────────────────────────────────────────────────────────
public class OrderSuccessViewModel
{
    public string OrderCode { get; set; } = string.Empty;
    public decimal FinalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public string ShippingName { get; set; } = string.Empty;
    public string ShippingPhone { get; set; } = string.Empty;
    public string ShippingAddr { get; set; } = string.Empty;
    public int ItemCount { get; set; }
}

// ─────────────────────────────────────────────────────────────
// ORDER HISTORY & DETAIL
// ─────────────────────────────────────────────────────────────
public class OrderHistoryViewModel
{
    public List<OrderSummaryViewModel> Orders { get; set; } = [];
}

public class OrderSummaryViewModel
{
    public int OrderID { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal FinalAmount { get; set; }
    public int ItemCount { get; set; }
}

public class OrderDetailViewModel
{
    public int OrderID { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string? TrackingNumber { get; set; }
    public DateTime OrderDate { get; set; }
    public string OrderEmail { get; set; } = string.Empty;
    public string ShippingName { get; set; } = string.Empty;
    public string ShippingPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public List<OrderDetailItemViewModel> Items { get; set; } = [];
    public List<OrderStatusHistoryViewModel> StatusHistory { get; set; } = [];
    public string? CouponCode { get; set; }
}

public class OrderDetailItemViewModel
{
    public string ProductName { get; set; } = string.Empty;
    public string SizeCode { get; set; } = string.Empty;
    public string ColorName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal SubTotal { get; set; }
}

public class OrderStatusHistoryViewModel
{
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? ActionType { get; set; }
    public string? Note { get; set; }
    public DateTime? ChangedAt { get; set; }
    public DateTime? ChangedAtLocal { get; set; }
    public string? ChangedByName { get; set; }
}
