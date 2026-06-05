using ClothingStore.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClothingStore.Data;

public class StoreDbContext(DbContextOptions<StoreDbContext> options) : DbContext(options)
{
    public DbSet<DiscountProgram> DiscountPrograms => Set<DiscountProgram>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Color> Colors => Set<Color>();
    public DbSet<Size> Sizes => Set<Size>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<AccountRole> AccountRoles => Set<AccountRole>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<ShippingAddress> ShippingAddresses => Set<ShippingAddress>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<CouponUsage> CouponUsages => Set<CouponUsage>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptDetail> GoodsReceiptDetails => Set<GoodsReceiptDetail>();
    public DbSet<GoodsIssue> GoodsIssues => Set<GoodsIssue>();
    public DbSet<GoodsIssueDetail> GoodsIssueDetails => Set<GoodsIssueDetail>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<PaymentTransaction> Transactions => Set<PaymentTransaction>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ReviewImage> ReviewImages => Set<ReviewImage>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductRelationship> ProductRelationships => Set<ProductRelationship>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── CATEGORIES ──────────────────────────────────────────────
        modelBuilder.Entity<Category>()
            .HasOne(x => x.ParentCategory)
            .WithMany(x => x.ChildCategories)
            .HasForeignKey(x => x.ParentCategoryID);

        // ── PRODUCTS ─────────────────────────────────────────────────
        modelBuilder.Entity<Product>()
            .ToTable(tb => tb.HasTrigger("TR_Products_UpdatedAt"))
            .HasIndex(x => x.Slug).IsUnique();

        modelBuilder.Entity<Product>()
            .HasIndex(x => x.CategoryID)
            .HasDatabaseName("IX_Products_CategoryId");

        modelBuilder.Entity<Product>()
            .HasOne(x => x.Category)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryID);

        modelBuilder.Entity<Product>()
            .HasOne(x => x.DiscountProgram)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.ProgramID);

        // ── PRODUCT RELATIONSHIPS ────────────────────────────────────
        modelBuilder.Entity<ProductRelationship>()
            .HasKey(x => new { x.ProductID, x.LinkedProductID });

        modelBuilder.Entity<ProductRelationship>()
            .HasOne(x => x.Product)
            .WithMany(x => x.RelatedProducts)
            .HasForeignKey(x => x.ProductID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductRelationship>()
            .HasOne(x => x.LinkedProduct)
            .WithMany(x => x.RelatedByProducts)
            .HasForeignKey(x => x.LinkedProductID)
            .OnDelete(DeleteBehavior.Restrict);

        // ── PRODUCT VARIANTS ─────────────────────────────────────────
        modelBuilder.Entity<ProductVariant>()
            .ToTable(tb => tb.HasTrigger("TR_ProductVariants_UpdatedAt"))
            .HasIndex(x => x.SKU).IsUnique();

        modelBuilder.Entity<ProductVariant>()
            .HasIndex(x => new { x.ProductID, x.SizeID, x.ColorID })
            .IsUnique()
            .HasDatabaseName("UQ_ProductVariant");

        modelBuilder.Entity<ProductVariant>()
            .HasIndex(x => x.ProductID)
            .HasDatabaseName("IX_ProductVariants_ProductId");

        // New indexes (DB-06)
        modelBuilder.Entity<ProductVariant>()
            .HasIndex(x => x.ColorID)
            .HasDatabaseName("IX_ProductVariants_ColorId");

        modelBuilder.Entity<ProductVariant>()
            .HasIndex(x => x.SizeID)
            .HasDatabaseName("IX_ProductVariants_SizeId");

        modelBuilder.Entity<ProductVariant>()
            .HasOne(x => x.Product)
            .WithMany(x => x.ProductVariants)
            .HasForeignKey(x => x.ProductID);

        modelBuilder.Entity<ProductVariant>()
            .HasOne(x => x.Size)
            .WithMany(x => x.ProductVariants)
            .HasForeignKey(x => x.SizeID);

        modelBuilder.Entity<ProductVariant>()
            .HasOne(x => x.Color)
            .WithMany(x => x.ProductVariants)
            .HasForeignKey(x => x.ColorID);

        // ── CUSTOMERS ────────────────────────────────────────────────
        modelBuilder.Entity<Customer>()
            .HasOne(x => x.Membership)
            .WithMany(x => x.Customers)
            .HasForeignKey(x => x.MembershipID);

        // ── ACCOUNTS ─────────────────────────────────────────────────
        modelBuilder.Entity<Account>()
            .ToTable(tb => tb.HasTrigger("TR_Accounts_UpdatedAt"))
            .HasIndex(x => x.CustomerId)
            .IsUnique()
            .HasFilter("[CustomerId] IS NOT NULL");

        modelBuilder.Entity<Account>()
            .HasOne(x => x.Customer)
            .WithOne(x => x.Account)
            .HasForeignKey<Account>(x => x.CustomerId);

        // ── ACCOUNT ROLES ────────────────────────────────────────────
        modelBuilder.Entity<AccountRole>()
            .HasKey(x => new { x.UserId, x.RoleId });

        modelBuilder.Entity<AccountRole>()
            .HasOne(x => x.Account)
            .WithMany(x => x.AccountRoles)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AccountRole>()
            .HasOne(x => x.Role)
            .WithMany(x => x.AccountRoles)
            .HasForeignKey(x => x.RoleId);

        // ── CARTS ────────────────────────────────────────────────────
        modelBuilder.Entity<Cart>()
            .ToTable(tb => tb.HasTrigger("TR_Carts_UpdatedAt"))
            .HasIndex(x => x.CustomerId)
            .HasDatabaseName("IX_Carts_CustomerId");

        modelBuilder.Entity<Cart>()
            .HasIndex(x => x.CustomerId)
            .IsUnique()
            .HasFilter("[CustomerId] IS NOT NULL AND [Status] = 'Active'")
            .HasDatabaseName("UX_Cart_Active");

        modelBuilder.Entity<Cart>()
            .HasIndex(x => x.SessionKey)
            .IsUnique()
            .HasFilter("[SessionKey] IS NOT NULL")
            .HasDatabaseName("UX_Carts_SessionKey");

        modelBuilder.Entity<Cart>()
            .HasOne(x => x.Customer)
            .WithMany(x => x.Carts)
            .HasForeignKey(x => x.CustomerId);

        // ── CART ITEMS ───────────────────────────────────────────────
        modelBuilder.Entity<CartItem>()
            .HasIndex(x => new { x.CartID, x.VariantID })
            .IsUnique()
            .HasDatabaseName("UQ_CartVariant");

        modelBuilder.Entity<CartItem>()
            .HasOne(x => x.Cart)
            .WithMany(x => x.CartItems)
            .HasForeignKey(x => x.CartID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CartItem>()
            .HasOne(x => x.ProductVariant)
            .WithMany(x => x.CartItems)
            .HasForeignKey(x => x.VariantID);

        // ── COUPONS ──────────────────────────────────────────────────
        modelBuilder.Entity<Coupon>()
            .ToTable(tb => tb.HasTrigger("TR_Coupons_UpdatedAt"));

        // ── SHIPPING ADDRESSES ───────────────────────────────────────
        modelBuilder.Entity<ShippingAddress>()
            .HasIndex(x => x.CustomerId)
            .HasDatabaseName("IX_ShippingAddresses_CustomerId");

        modelBuilder.Entity<ShippingAddress>()
            .HasIndex(x => x.CustomerId)
            .IsUnique()
            .HasFilter("[IsDefault] = 1")
            .HasDatabaseName("UX_DefaultAddress");

        modelBuilder.Entity<ShippingAddress>()
            .HasOne(x => x.Customer)
            .WithMany(x => x.ShippingAddresses)
            .HasForeignKey(x => x.CustomerId);

        // ── ORDERS ───────────────────────────────────────────────────
        modelBuilder.Entity<Order>()
            .ToTable(tb => tb.HasTrigger("TR_Orders_UpdatedAt"))
            .HasIndex(x => x.OrderCode).IsUnique();

        modelBuilder.Entity<Order>()
            .HasIndex(x => x.CartID)
            .IsUnique()
            .HasDatabaseName("UX_Order_Cart");

        // New indexes (DB-06)
        modelBuilder.Entity<Order>()
            .HasIndex(x => new { x.OrderStatus, x.OrderDate })
            .HasDatabaseName("IX_Orders_OrderStatus");

        modelBuilder.Entity<Order>()
            .HasIndex(x => new { x.CustomerId, x.OrderDate })
            .HasDatabaseName("IX_Orders_CustomerId");

        modelBuilder.Entity<Order>()
            .HasOne(x => x.Customer)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.CustomerId);

        modelBuilder.Entity<Order>()
            .HasOne(x => x.Cart)
            .WithOne(x => x.Order)
            .HasForeignKey<Order>(x => x.CartID);

        // ── COUPON USAGES ────────────────────────────────────────────
        modelBuilder.Entity<CouponUsage>()
            .HasIndex(x => new { x.CouponID, x.OrderID })
            .IsUnique()
            .HasDatabaseName("UQ_Coupon_Order");

        modelBuilder.Entity<CouponUsage>()
            .HasIndex(x => x.CouponID)
            .HasDatabaseName("IX_CouponUsages_CouponId");

        modelBuilder.Entity<CouponUsage>()
            .HasOne(x => x.Coupon)
            .WithMany(x => x.CouponUsages)
            .HasForeignKey(x => x.CouponID);

        modelBuilder.Entity<CouponUsage>()
            .HasOne(x => x.Customer)
            .WithMany(x => x.CouponUsages)
            .HasForeignKey(x => x.CustomerId);

        modelBuilder.Entity<CouponUsage>()
            .HasOne(x => x.Order)
            .WithMany(x => x.CouponUsages)
            .HasForeignKey(x => x.OrderID);

        // ── ORDER DETAILS ────────────────────────────────────────────
        modelBuilder.Entity<OrderDetail>()
            .HasIndex(x => new { x.OrderID, x.VariantID })
            .IsUnique()
            .HasDatabaseName("UQ_OrderVariant");

        modelBuilder.Entity<OrderDetail>()
            .HasIndex(x => x.VariantID)
            .HasDatabaseName("IX_OrderDetails_VariantId");

        modelBuilder.Entity<OrderDetail>()
            .HasOne(x => x.Order)
            .WithMany(x => x.OrderDetails)
            .HasForeignKey(x => x.OrderID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderDetail>()
            .HasOne(x => x.ProductVariant)
            .WithMany(x => x.OrderDetails)
            .HasForeignKey(x => x.VariantID);

        // ── ORDER STATUS HISTORY ─────────────────────────────────────
        modelBuilder.Entity<OrderStatusHistory>()
            .HasOne(x => x.Order)
            .WithMany(x => x.StatusHistory)
            .HasForeignKey(x => x.OrderID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderStatusHistory>()
            .HasOne(x => x.ChangedByAccount)
            .WithMany(x => x.OrderStatusChanges)
            .HasForeignKey(x => x.ChangedBy);

        // ── GOODS RECEIPTS ───────────────────────────────────────────
        modelBuilder.Entity<GoodsReceipt>()
            .HasOne(x => x.Supplier)
            .WithMany(x => x.GoodsReceipts)
            .HasForeignKey(x => x.SupplierID);

        modelBuilder.Entity<GoodsReceiptDetail>()
            .HasIndex(x => new { x.ReceiptID, x.VariantID })
            .IsUnique()
            .HasDatabaseName("UQ_ReceiptVariant");

        modelBuilder.Entity<GoodsReceiptDetail>()
            .HasOne(x => x.GoodsReceipt)
            .WithMany(x => x.GoodsReceiptDetails)
            .HasForeignKey(x => x.ReceiptID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GoodsReceiptDetail>()
            .HasOne(x => x.ProductVariant)
            .WithMany(x => x.GoodsReceiptDetails)
            .HasForeignKey(x => x.VariantID);

        // ── GOODS ISSUES ─────────────────────────────────────────────
        modelBuilder.Entity<GoodsIssue>()
            .HasOne(x => x.Order)
            .WithMany(x => x.GoodsIssues)
            .HasForeignKey(x => x.OrderID);

        modelBuilder.Entity<GoodsIssueDetail>()
            .HasIndex(x => new { x.IssueID, x.VariantID })
            .IsUnique()
            .HasDatabaseName("UQ_IssueVariant");

        modelBuilder.Entity<GoodsIssueDetail>()
            .HasOne(x => x.GoodsIssue)
            .WithMany(x => x.GoodsIssueDetails)
            .HasForeignKey(x => x.IssueID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GoodsIssueDetail>()
            .HasOne(x => x.ProductVariant)
            .WithMany(x => x.GoodsIssueDetails)
            .HasForeignKey(x => x.VariantID);

        // ── INVENTORY TRANSACTIONS ───────────────────────────────────
        modelBuilder.Entity<InventoryTransaction>()
            .HasIndex(x => new { x.VariantID, x.TransactionType })
            .HasDatabaseName("IX_InventoryTx_VariantId");

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(x => x.ProductVariant)
            .WithMany(x => x.InventoryTransactions)
            .HasForeignKey(x => x.VariantID);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(x => x.CreatedByAccount)
            .WithMany(x => x.InventoryTransactions)
            .HasForeignKey(x => x.CreatedBy);

        // ── PAYMENT TRANSACTIONS ─────────────────────────────────────
        modelBuilder.Entity<PaymentTransaction>()
            .HasIndex(x => x.ExternalID).IsUnique();

        modelBuilder.Entity<PaymentTransaction>()
            .HasIndex(x => x.OrderID)
            .IsUnique()
            .HasFilter("[Status] = 'Success'")
            .HasDatabaseName("UX_One_Success_Transaction_Per_Order");

        modelBuilder.Entity<PaymentTransaction>()
            .HasOne(x => x.Order)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.OrderID);

        // ── REVIEWS ──────────────────────────────────────────────────
        modelBuilder.Entity<Review>()
            .HasIndex(x => x.ProductID)
            .HasDatabaseName("IX_Reviews_ProductId");

        modelBuilder.Entity<Review>()
            .HasIndex(x => new { x.OrderID, x.ProductID })
            .IsUnique()
            .HasDatabaseName("UQ_Order_Product_Review");

        modelBuilder.Entity<Review>()
            .HasOne(x => x.Order)
            .WithMany(x => x.Reviews)
            .HasForeignKey(x => x.OrderID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Review>()
            .HasOne(x => x.Product)
            .WithMany(x => x.Reviews)
            .HasForeignKey(x => x.ProductID);

        modelBuilder.Entity<Review>()
            .HasOne(x => x.ProductVariant)
            .WithMany()
            .HasForeignKey(x => x.VariantID);

        modelBuilder.Entity<Review>()
            .HasOne(x => x.Customer)
            .WithMany(x => x.Reviews)
            .HasForeignKey(x => x.CustomerId);

        modelBuilder.Entity<ReviewImage>()
            .HasOne(x => x.Review)
            .WithMany(x => x.ReviewImages)
            .HasForeignKey(x => x.ReviewID)
            .OnDelete(DeleteBehavior.Cascade);

        // ── CHAT ─────────────────────────────────────────────────────
        modelBuilder.Entity<ChatSession>()
            .HasOne(x => x.Account)
            .WithMany(x => x.ChatSessions)
            .HasForeignKey(x => x.UserId);

        modelBuilder.Entity<ChatMessage>()
            .HasOne(x => x.ChatSession)
            .WithMany(x => x.ChatMessages)
            .HasForeignKey(x => x.SessionID)
            .OnDelete(DeleteBehavior.Cascade);

        // ── PRODUCT IMAGES ───────────────────────────────────────────
        modelBuilder.Entity<ProductImage>()
            .HasIndex(x => new { x.VariantID, x.DisplayOrder })
            .IsUnique()
            .HasDatabaseName("UX_ProductImage_Order");

        modelBuilder.Entity<ProductImage>()
            .HasOne(x => x.ProductVariant)
            .WithMany(x => x.ProductImages)
            .HasForeignKey(x => x.VariantID);
    }
}
