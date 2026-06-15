using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ClothingStore.Models.Entities;

// ─────────────────────────────────────────────
// Constants for domain string values
// ─────────────────────────────────────────────
public static class OrderStatus
{
    public const string Pending        = "Pending";
    public const string Confirmed      = "Confirmed";
    public const string Processing     = "Processing";
    public const string ReadyToShip    = "ReadyToShip";
    public const string Shipping       = "Shipping";
    public const string DeliveredPendingCOD = "DeliveredPendingCOD";
    public const string Delivered      = "Delivered";
    public const string DeliveryAttemptFailed = "DeliveryAttemptFailed";
    public const string DeliveryFailed = "DeliveryFailed";
    public const string Returned       = "Returned";
    public const string Cancelled      = "Cancelled";
}

public static class PaymentStatus
{
    public const string Unpaid        = "Unpaid";
    public const string Paid          = "Paid";
    public const string Refunded      = "Refunded";
    public const string PartialRefund = "PartialRefund";
}

public static class PaymentMethod
{
    public const string COD          = "COD";
    public const string Online       = "Online";
    public const string BankTransfer = "BankTransfer";
    public const string MoMo         = "MoMo";
    public const string ZaloPay      = "ZaloPay";
}

public static class CartStatus
{
    public const string Active    = "Active";
    public const string Ordered   = "Ordered";
    public const string Abandoned = "Abandoned";
}

public static class AccountStatus
{
    public const string Active   = "Active";
    public const string Inactive = "Inactive";
    public const string Banned   = "Banned";
}

public static class DiscountType
{
    public const string Percentage  = "Percentage";
    public const string FixedAmount = "FixedAmount";
}

public static class InventoryTransactionType
{
    public const string Sale       = "Sale";
    public const string Receipt    = "Receipt";
    public const string Return     = "Return";
    public const string Adjustment = "Adjustment";
    public const string Issue      = "Issue";
}

// ─────────────────────────────────────────────
// Entity Classes
// ─────────────────────────────────────────────

[Table("DISCOUNTPROGRAMS")]
public class DiscountProgram
{
    [Key] public int ProgramID { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public int DiscountPercent { get; set; }
    [Column(TypeName = "date")] public DateTime StartDate { get; set; }
    [Column(TypeName = "date")] public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    // Navigation
    public ICollection<Product> Products { get; set; } = new List<Product>();

    // Computed — not mapped
    [NotMapped]
    public bool IsCurrentlyActive =>
        IsActive &&
        StartDate.Date <= DateTime.Today &&
        EndDate.Date >= DateTime.Today;
}

[Table("DISCOUNTPROGRAMAUDITS")]
public class DiscountProgramAudit
{
    [Key] public int AuditID { get; set; }
    public int ProgramID { get; set; }
    public string ActionType { get; set; } = string.Empty; // CreateProgram, UpdateProgram, ActivateProgram, DeactivateProgram
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public int ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public DiscountProgram DiscountProgram { get; set; } = null!;
    public Account ChangedByAccount { get; set; } = null!;
}


[Table("CATEGORIES")]
public class Category
{
    [Key] public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? ParentCategoryID { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Category? ParentCategory { get; set; }
    public ICollection<Category> ChildCategories { get; set; } = new List<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

[Table("PRODUCTS")]
public class Product
{
    [Key] public int ProductID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    [MaxLength(500)] public string? SearchNormalizedName { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? Description { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string FitType { get; set; } = string.Empty;
    public string CareInstructions { get; set; } = string.Empty;
    public int CategoryID { get; set; }
    public int? ProgramID { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsBestSeller { get; set; } = false;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Category Category { get; set; } = null!;
    public DiscountProgram? DiscountProgram { get; set; }
    public ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    public ICollection<ProductRelationship> RelatedProducts { get; set; } = new List<ProductRelationship>();
    public ICollection<ProductRelationship> RelatedByProducts { get; set; } = new List<ProductRelationship>();
}

[Table("PRODUCTRELATIONSHIPS")]
public class ProductRelationship
{
    public int ProductID { get; set; }
    public int LinkedProductID { get; set; }

    public Product Product { get; set; } = null!;
    public Product LinkedProduct { get; set; } = null!;
}

[Table("COLORS")]
public class Color
{
    [Key] public int ColorID { get; set; }
    public string ColorName { get; set; } = string.Empty;
    public string HexCode { get; set; } = "#000000";
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
}

[Table("SIZES")]
public class Size
{
    [Key] public int SizeID { get; set; }
    public string SizeCode { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    // Added: correct sort order (XS=1, S=2, M=3, L=4, XL=5, XXL=6)
    public int SortOrder { get; set; } = 99;

    // Navigation
    public ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
}

[Table("PRODUCTVARIANTS")]
public class ProductVariant
{
    [Key] public int VariantID { get; set; }
    public string SKU { get; set; } = string.Empty;
    [Column(TypeName = "numeric(18,2)")] public decimal SellingPrice { get; set; }
    public int StockQuantity { get; set; }
    public int ProductID { get; set; }
    public int SizeID { get; set; }
    public int ColorID { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Product Product { get; set; } = null!;
    public Size Size { get; set; } = null!;
    public Color Color { get; set; } = null!;
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
    public ICollection<GoodsReceiptDetail> GoodsReceiptDetails { get; set; } = new List<GoodsReceiptDetail>();
    public ICollection<GoodsIssueDetail> GoodsIssueDetails { get; set; } = new List<GoodsIssueDetail>();
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    /// <summary>
    /// Compute effective price after applying active DiscountProgram.
    /// </summary>
    [NotMapped]
    public decimal EffectivePrice
    {
        get
        {
            var dp = Product?.DiscountProgram;
            if (dp is { IsCurrentlyActive: true })
                return Math.Round(SellingPrice * (1 - dp.DiscountPercent / 100m), 2);
            return SellingPrice;
        }
    }

    [NotMapped]
    public bool HasDiscount =>
        Product?.DiscountProgram is { IsCurrentlyActive: true };
}

[Table("MEMBERSHIPS")]
public class Membership
{
    [Key] public int MembershipID { get; set; }
    public string MembershipName { get; set; } = string.Empty;
    public int MinPoint { get; set; }
    public int? MaxPoint { get; set; }
    [Column(TypeName = "numeric(5,2)")] public decimal DiscountPercent { get; set; }

    // Navigation
    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
}

[Table("CUSTOMERS")]
public class Customer
{
    [Key] public int CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int RewardPoints { get; set; }
    public int TotalOrders { get; set; }
    public int MembershipID { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Membership Membership { get; set; } = null!;
    public Account? Account { get; set; }
    public ICollection<Cart> Carts { get; set; } = new List<Cart>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<ShippingAddress> ShippingAddresses { get; set; } = new List<ShippingAddress>();
    public ICollection<CouponUsage> CouponUsages { get; set; } = new List<CouponUsage>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<ReviewHelpfulVote> ReviewHelpfulVotes { get; set; } = new List<ReviewHelpfulVote>();
}

[Table("ACCOUNTS")]
public class Account
{
    [Key] public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Status { get; set; } = AccountStatus.Active;
    public int? CustomerId { get; set; }
    // Added audit fields (DB-03)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    // Navigation
    public Customer? Customer { get; set; }
    public ICollection<AccountRole> AccountRoles { get; set; } = new List<AccountRole>();
    public ICollection<OrderStatusHistory> OrderStatusChanges { get; set; } = new List<OrderStatusHistory>();
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
    public ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();

    // Helper
    [NotMapped]
    public IEnumerable<string> Roles =>
        AccountRoles.Select(ar => ar.Role?.Name ?? string.Empty).Where(n => !string.IsNullOrEmpty(n));
}

[Table("ROLES")]
public class Role
{
    [Key] public int RoleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;

    // Navigation
    public ICollection<AccountRole> AccountRoles { get; set; } = new List<AccountRole>();
}

[Table("ACCOUNTROLES")]
public class AccountRole
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public Account Account { get; set; } = null!;
    public Role Role { get; set; } = null!;
}

[Table("CARTS")]
public class Cart
{
    [Key] public int CartID { get; set; }
    public string Status { get; set; } = CartStatus.Active;
    public string? SessionKey { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? CustomerId { get; set; }

    // Navigation
    public Customer? Customer { get; set; }
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public Order? Order { get; set; }
}

[Table("CARTITEMS")]
public class CartItem
{
    [Key] public int CartItemID { get; set; }
    public int Quantity { get; set; }
    public int CartID { get; set; }
    public int VariantID { get; set; }
    public DateTime? AddedAt { get; set; }

    // Navigation
    public Cart Cart { get; set; } = null!;
    public ProductVariant ProductVariant { get; set; } = null!;
}

[Table("COUPONS")]
public class Coupon
{
    [Key] public int CouponID { get; set; }
    public string CouponCode { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    [Column(TypeName = "numeric(18,2)")] public decimal DiscountValue { get; set; }
    [Column(TypeName = "numeric(18,2)")] public decimal MinOrderValue { get; set; }
    public int? UsageLimit { get; set; }
    // Added: UsedCount for fast limit check (DB-04)
    public int UsedCount { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    // Navigation
    public ICollection<CouponUsage> CouponUsages { get; set; } = new List<CouponUsage>();

    // Helpers
    [NotMapped]
    public bool IsCurrentlyValid =>
        IsActive &&
        DateTime.UtcNow >= ValidFrom &&
        DateTime.UtcNow <= ValidTo &&
        (UsageLimit == null || UsedCount < UsageLimit);

    /// <summary>
    /// Calculate discount amount for a given order subtotal.
    /// </summary>
    public decimal CalculateDiscount(decimal orderSubTotal)
    {
        if (!IsCurrentlyValid || orderSubTotal < MinOrderValue) return 0;
        return DiscountType == Entities.DiscountType.Percentage
            ? Math.Round(orderSubTotal * DiscountValue / 100m, 2)
            : Math.Min(DiscountValue, orderSubTotal);
    }
}

[Table("SHIPPINGADDRESSES")]
public class ShippingAddress
{
    [Key] public int AddressID { get; set; }
    [MaxLength(50)] public string? AddressName { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string ReceiverPhone { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public int? ProvinceId { get; set; }
    public int? DistrictId { get; set; }
    public int? WardId { get; set; }
    public string? Note { get; set; }
    public bool IsDefault { get; set; }
    [Column(TypeName = "numeric(9,6)")] public decimal? Latitude { get; set; }
    [Column(TypeName = "numeric(9,6)")] public decimal? Longitude { get; set; }
    public int CustomerId { get; set; }

    // Navigation
    public Customer Customer { get; set; } = null!;

    // Helper
    [NotMapped]
    public string FullAddress =>
        string.IsNullOrWhiteSpace(AddressLine) 
            ? $"{Ward}, {District}, {Province}{(string.IsNullOrWhiteSpace(Note) ? "" : $". Ghi chú: {Note}")}".Trim(',', ' ')
            : $"{AddressLine}, {Ward}, {District}, {Province}{(string.IsNullOrWhiteSpace(Note) ? "" : $". Ghi chú: {Note}")}".Trim(',', ' ');
}

[Table("ORDERS")]
[Index(nameof(CustomerId), nameof(OrderDate), IsUnique = false, Name = "IX_Orders_CustomerId_OrderDate")]
[Index(nameof(ShippingAddressId), IsUnique = false, Name = "IX_Orders_ShippingAddressId")]
public class Order
{
    [Key] public int OrderID { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string? TrackingNumber { get; set; }
    public DateTime OrderDate { get; set; }
    public string OrderEmail { get; set; } = string.Empty;
    // Snapshot address
    public int? ShippingAddressId { get; set; }
    public string ShippingRecipientName { get; set; } = string.Empty;
    public string ShippingPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingWard { get; set; } = string.Empty;
    public string ShippingDistrict { get; set; } = string.Empty;
    public string ShippingProvince { get; set; } = string.Empty;
    public string ShippingFullAddress { get; set; } = string.Empty;
    public string? DeliveryNote { get; set; }
    // Finance & status
    public string PaymentStatus { get; set; } = Entities.PaymentStatus.Unpaid;
    public string PaymentMethod { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = Entities.OrderStatus.Pending;
    [Column(TypeName = "numeric(18,2)")] public decimal TotalAmount { get; set; }
    [Column(TypeName = "numeric(18,2)")] public decimal ShippingFee { get; set; }
    [Column(TypeName = "numeric(18,2)")] public decimal DiscountAmount { get; set; }
    [Column(TypeName = "numeric(18,2)")] public decimal FinalAmount { get; set; }
    public int? CustomerId { get; set; }
    public int CartID { get; set; }

    // Navigation
    public Customer? Customer { get; set; }
    public Cart Cart { get; set; } = null!;
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public ICollection<CouponUsage> CouponUsages { get; set; } = new List<CouponUsage>();
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
    public ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();
    public ICollection<GoodsIssue> GoodsIssues { get; set; } = new List<GoodsIssue>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    // Shipper Fields
    public int? AssignedShipperId { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? ShippingStartedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? DeliveryFailureReasonCode { get; set; }
    public string? DeliveryFailureReason { get; set; }
    public int DeliveryAttemptCount { get; set; }
    public DateTime? NextDeliveryDate { get; set; }
    public string? DeliveryRescheduleReason { get; set; }
    public DateTime? LastDeliveryAttemptAt { get; set; }
    public Account? AssignedShipper { get; set; }

    // Helper
    // Full address is now stored in DB via ShippingFullAddress so we don't strictly need this, but keeping it for compatibility if needed.
    // We can just rely on the stored field.
}

[Table("COUPONUSAGES")]
public class CouponUsage
{
    [Key] public int UsageID { get; set; }
    public int CouponID { get; set; }
    public int? CustomerId { get; set; }
    public int OrderID { get; set; }
    public DateTime UsedAt { get; set; }

    // Navigation
    public Coupon Coupon { get; set; } = null!;
    public Customer? Customer { get; set; }
    public Order Order { get; set; } = null!;
}

[Table("ORDERDETAILS")]
public class OrderDetail
{
    [Key] public int OrderDetailID { get; set; }
    public int Quantity { get; set; }
    [Column(TypeName = "numeric(18,2)")] public decimal UnitPrice { get; set; }
    public int OrderID { get; set; }
    public int VariantID { get; set; }
    // Snapshot columns (DB-01 — preserves history even if variant changes)
    public string ProductNameSnapshot { get; set; } = string.Empty;
    public string SizeCodeSnapshot { get; set; } = string.Empty;
    public string ColorNameSnapshot { get; set; } = string.Empty;
    public string SKUSnapshot { get; set; } = string.Empty;

    // Navigation
    public Order Order { get; set; } = null!;
    public ProductVariant ProductVariant { get; set; } = null!;

    [NotMapped]
    public decimal SubTotal => UnitPrice * Quantity;
}

[Table("ORDERSTATUSHISTORY")]
public class OrderStatusHistory
{
    [Key] public int HistoryID { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? ActionType { get; set; }
    public string? Note { get; set; }
    public DateTime? ChangedAt { get; set; }
    public int? ChangedBy { get; set; }
    public int OrderID { get; set; }

    // Navigation
    public Order Order { get; set; } = null!;
    public Account? ChangedByAccount { get; set; }
}

[Table("SUPPLIERS")]
public class Supplier
{
    [Key] public int SupplierID { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<GoodsReceipt> GoodsReceipts { get; set; } = new List<GoodsReceipt>();
}

[Table("GOODSRECEIPTS")]
public class GoodsReceipt
{
    [Key] public int ReceiptID { get; set; }
    public DateTime ReceiptDate { get; set; }
    [Column(TypeName = "numeric(18,2)")] public decimal TotalCost { get; set; }
    public int SupplierID { get; set; }

    // Navigation
    public Supplier Supplier { get; set; } = null!;
    public ICollection<GoodsReceiptDetail> GoodsReceiptDetails { get; set; } = new List<GoodsReceiptDetail>();
}

[Table("GOODSRECEIPTDETAILS")]
public class GoodsReceiptDetail
{
    [Key] public int ReceiptDetailID { get; set; }
    public int Quantity { get; set; }
    [Column(TypeName = "numeric(18,2)")] public decimal ImportPrice { get; set; }
    public int ReceiptID { get; set; }
    public int VariantID { get; set; }

    // Navigation
    public GoodsReceipt GoodsReceipt { get; set; } = null!;
    public ProductVariant ProductVariant { get; set; } = null!;
}

[Table("GOODSISSUES")]
public class GoodsIssue
{
    [Key] public int IssueID { get; set; }
    public DateTime IssueDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    [Column(TypeName = "numeric(18,2)")] public decimal TotalValue { get; set; }
    public int? OrderID { get; set; }

    // Navigation
    public Order? Order { get; set; }
    public ICollection<GoodsIssueDetail> GoodsIssueDetails { get; set; } = new List<GoodsIssueDetail>();
}

[Table("GOODSISSUEDETAILS")]
public class GoodsIssueDetail
{
    [Key] public int IssueDetailID { get; set; }
    public int Quantity { get; set; }
    [Column(TypeName = "numeric(18,2)")] public decimal UnitPrice { get; set; }
    public int IssueID { get; set; }
    public int VariantID { get; set; }

    // Navigation
    public GoodsIssue GoodsIssue { get; set; } = null!;
    public ProductVariant ProductVariant { get; set; } = null!;
}

[Table("INVENTORYTRANSACTIONS")]
public class InventoryTransaction
{
    [Key] public int TransactionID { get; set; }
    public int VariantID { get; set; }
    public int Quantity { get; set; }
    public int StockAfterTransaction { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string? ReferenceID { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }

    // Navigation
    public ProductVariant ProductVariant { get; set; } = null!;
    public Account? CreatedByAccount { get; set; }
}

[Table("TRANSACTIONS")]
public class PaymentTransaction
{
    [Key] public int TransactionID { get; set; }
    public string ExternalID { get; set; } = string.Empty;
    [Column(TypeName = "numeric(18,2)")] public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string PaymentGateway { get; set; } = string.Empty;
    public int OrderID { get; set; }

    // Navigation
    public Order Order { get; set; } = null!;
}

[Table("REVIEWS")]
public class Review
{
    [Key] public int ReviewID { get; set; }
    public string Comment { get; set; } = string.Empty;
    public int Rating { get; set; }
    public DateTime ReviewDate { get; set; }
    public bool IsApproved { get; set; }
    public int OrderID { get; set; }
    public int ProductID { get; set; }
    public int? VariantID { get; set; }
    public int CustomerId { get; set; }

    // Navigation
    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ProductVariant? ProductVariant { get; set; }
    public Customer Customer { get; set; } = null!;
    public ICollection<ReviewImage> ReviewImages { get; set; } = new List<ReviewImage>();
    public ICollection<ReviewHelpfulVote> ReviewHelpfulVotes { get; set; } = new List<ReviewHelpfulVote>();
}

[Table("REVIEWHELPFULVOTES")]
public class ReviewHelpfulVote
{
    [Key] public int VoteID { get; set; }
    public int ReviewID { get; set; }
    public int CustomerId { get; set; }
    public DateTime VotedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Review Review { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
}

[Table("REVIEWIMAGES")]
public class ReviewImage
{
    [Key] public int ImageID { get; set; }
    public string ImageURL { get; set; } = string.Empty;
    public int ReviewID { get; set; }

    // Navigation
    public Review Review { get; set; } = null!;
}

[Table("CHATSESSIONS")]
public class ChatSession
{
    [Key] public int SessionID { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int UserId { get; set; }

    // Navigation
    public Account Account { get; set; } = null!;
    public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
}

[Table("CHATMESSAGES")]
public class ChatMessage
{
    [Key] public int MessageID { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool IsFromUser { get; set; }
    public int SessionID { get; set; }

    // Navigation
    public ChatSession ChatSession { get; set; } = null!;
}

[Table("PRODUCTIMAGES")]
public class ProductImage
{
    [Key] public int ImageID { get; set; }
    public int DisplayOrder { get; set; } = 1;
    public string ImageURL { get; set; } = string.Empty;
    public int VariantID { get; set; }
    // Added: IsMain flag (DB-08)
    public bool IsMain { get; set; }

    // Navigation
    public ProductVariant ProductVariant { get; set; } = null!;
}
