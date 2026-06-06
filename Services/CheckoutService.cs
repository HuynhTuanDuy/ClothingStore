using ClothingStore.Data;
using ClothingStore.Models.Entities;
using ClothingStore.Models.ViewModels;
using ClothingStore.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ClothingStore.Services;

public class CheckoutService(
    StoreDbContext dbContext,
    ICartService cartService,
    ICouponService couponService,
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    ICurrentCustomerService currentCustomerService,
    IHttpContextAccessor httpContextAccessor,
    IMemoryCache memoryCache) : ICheckoutService
{
    // Shipping fee policy
    private const decimal FreeShippingThreshold = 500_000m;
    private const decimal StandardShippingFee   = 30_000m;

    public async Task<CheckoutViewModel> GetCheckoutAsync(CheckoutInputModel? input = null)
    {
        return new CheckoutViewModel
        {
            Input = input ?? new CheckoutInputModel(),
            Cart  = await cartService.GetCartAsync()
        };
    }

    public async Task<CouponApplyResult> ApplyCouponAsync(string couponCode, decimal subTotal, int? customerId)
    {
        var result = await couponService.ValidateAsync(couponCode, subTotal, customerId);
        return new CouponApplyResult(result.IsValid, result.ErrorMessage, result.DiscountAmount, result.CouponCode);
    }

    public async Task<PlaceOrderResult> PlaceOrderAsync(CheckoutInputModel input)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync();
        var cart = await cartService.GetOrCreateActiveCartAsync();

        if (cart.CartItems.Count == 0)
            return PlaceOrderResult.Failure("Giỏ hàng của bạn đang trống.");

        // ── Validate stock ────────────────────────────────────────────
        foreach (var item in cart.CartItems)
        {
            if (!item.ProductVariant.IsActive || !item.ProductVariant.Product.IsActive)
                return PlaceOrderResult.Failure($"Sản phẩm '{item.ProductVariant.Product.ProductName}' không còn bán nữa.");

            if (item.Quantity > item.ProductVariant.StockQuantity)
                return PlaceOrderResult.Failure(
                    $"Không đủ hàng cho '{item.ProductVariant.Product.ProductName}' ({item.ProductVariant.Size.SizeCode}, {item.ProductVariant.Color.ColorName}).");
        }

        // ── Calculate totals with discount (BUG-04 FIX) ──────────────
        var subTotal = cart.CartItems.Sum(x => x.ProductVariant.EffectivePrice * x.Quantity);
        var shipping = subTotal >= FreeShippingThreshold ? 0m : StandardShippingFee;

        // ── Apply coupon (HIGH-01 FIX) ────────────────────────────────
        var customerId = currentCustomerService.GetCustomerId();
        decimal discountAmount = 0m;
        int?    couponId       = null;
        string? appliedCode    = null;

        if (!string.IsNullOrWhiteSpace(input.CouponCode))
        {
            var couponResult = await couponService.ValidateAsync(input.CouponCode, subTotal, customerId);
            if (!couponResult.IsValid)
                return PlaceOrderResult.Failure(couponResult.ErrorMessage ?? "Mã giảm giá không hợp lệ.");

            discountAmount = couponResult.DiscountAmount;
            couponId       = couponResult.CouponId;
            appliedCode    = couponResult.CouponCode;
        }

        var finalAmount = subTotal + shipping - discountAmount;

        // ── Build order code (LOW-02 FIX) ────────────────────────────
        var orderCode = GenerateOrderCode();

        var order = new Order
        {
            OrderCode            = orderCode,
            OrderDate            = DateTime.UtcNow,
            OrderEmail           = input.OrderEmail.Trim(),
            ShippingRecipientName = input.ShippingRecipientName.Trim(),
            ShippingPhone        = input.ShippingPhone.Trim(),
            ShippingAddress      = input.ShippingAddress.Trim(),
            ShippingWard         = input.ShippingWard.Trim(),
            ShippingDistrict     = input.ShippingDistrict.Trim(),
            ShippingProvince     = input.ShippingProvince.Trim(),
            PaymentMethod        = string.IsNullOrWhiteSpace(input.PaymentMethod) ? PaymentMethod.COD : input.PaymentMethod,
            PaymentStatus        = PaymentStatus.Unpaid,
            OrderStatus          = OrderStatus.Pending,
            TotalAmount          = subTotal,
            ShippingFee          = shipping,
            DiscountAmount       = discountAmount,
            FinalAmount          = finalAmount,
            CustomerId           = customerId,
            CartID               = cart.CartID
        };

        // ── Order details with snapshot (BUG-02 FIX) ─────────────────
        foreach (var item in cart.CartItems)
        {
            order.OrderDetails.Add(new OrderDetail
            {
                VariantID           = item.VariantID,
                Quantity            = item.Quantity,
                UnitPrice           = item.ProductVariant.EffectivePrice,
                // Snapshot — preserves history even if product/variant changes
                ProductNameSnapshot = item.ProductVariant.Product.ProductName,
                SizeCodeSnapshot    = item.ProductVariant.Size.SizeCode,
                ColorNameSnapshot   = item.ProductVariant.Color.ColorName,
                SKUSnapshot         = item.ProductVariant.SKU
            });

            // Deduct stock
            item.ProductVariant.StockQuantity -= item.Quantity;
            item.ProductVariant.UpdatedAt      = DateTime.UtcNow;

            // Log inventory transaction
            item.ProductVariant.InventoryTransactions.Add(new InventoryTransaction
            {
                Quantity             = -item.Quantity,
                StockAfterTransaction = item.ProductVariant.StockQuantity,
                TransactionType      = InventoryTransactionType.Sale,
                ReferenceID          = orderCode,
                CreatedAt            = DateTime.UtcNow
            });
        }

        // ── Record coupon usage (HIGH-01 FIX) ─────────────────────────
        if (couponId.HasValue && appliedCode is not null)
        {
            order.CouponUsages.Add(new CouponUsage
            {
                CouponID   = couponId.Value,
                CustomerId = customerId,
                UsedAt     = DateTime.UtcNow
            });

            // Increment UsedCount safely
            var couponToUpdate = await dbContext.Coupons.FindAsync(couponId.Value);
            if (couponToUpdate != null)
            {
                if (couponToUpdate.UsageLimit.HasValue && couponToUpdate.UsedCount >= couponToUpdate.UsageLimit.Value)
                {
                    return PlaceOrderResult.Failure("Mã giảm giá đã hết lượt sử dụng trong khi bạn đang thanh toán.");
                }
                couponToUpdate.UsedCount += 1;
                couponToUpdate.UpdatedAt = DateTime.UtcNow;
            }
        }

        // ── Initial status history ────────────────────────────────────
        order.StatusHistory.Add(new OrderStatusHistory
        {
            NewStatus = OrderStatus.Pending,
            Note      = "Đơn hàng được tạo từ trang thanh toán.",
            ChangedAt = DateTime.UtcNow
        });

        // ── Mark cart as ordered ──────────────────────────────────────
        cart.Status    = CartStatus.Ordered;
        cart.UpdatedAt = DateTime.UtcNow;

        // Clear session if guest order
        if (!customerId.HasValue)
        {
            cart.SessionKey = null;
            httpContextAccessor.HttpContext?.Session.Remove(CartService.SessionCartKey);
        }

        try
        {
            await orderRepository.AddOrderAsync(order);
            await unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            // Invalidate Best Seller cache since there is a new order
            memoryCache.Remove("BestSellerProducts_4");
            memoryCache.Remove("BestSellerProducts_12");
            memoryCache.Remove("ProductSalesDictionary");

            return PlaceOrderResult.Success(orderCode);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return PlaceOrderResult.Failure("Có lỗi xảy ra do nhiều người đặt hàng cùng lúc hoặc mã giảm giá vừa hết lượt. Vui lòng thử lại.");
        }
    }

    public async Task<OrderSuccessViewModel?> GetOrderSuccessAsync(string orderCode)
    {
        var order = await orderRepository.GetOrderByCodeAsync(orderCode);
        return order is null ? null : new OrderSuccessViewModel
        {
            OrderCode     = order.OrderCode,
            FinalAmount   = order.FinalAmount,
            PaymentMethod = order.PaymentMethod,
            OrderStatus   = order.OrderStatus,
            ShippingName  = order.ShippingRecipientName,
            ShippingPhone = order.ShippingPhone,
            ShippingAddr  = order.ShippingFullAddress,
            ItemCount     = order.OrderDetails.Count
        };
    }

    public async Task<PaymentPendingViewModel?> GetPaymentPendingAsync(string orderCode, string paymentMethod)
    {
        var order = await orderRepository.GetOrderByCodeAsync(orderCode);
        if (order is null) return null;

        var transferContent = Uri.EscapeDataString($"DH {order.OrderCode} {order.ShippingRecipientName}");
        var amountInt       = (long)Math.Round(order.FinalAmount);

        var vm = new PaymentPendingViewModel
        {
            OrderCode     = order.OrderCode,
            CustomerName  = order.ShippingRecipientName,
            FinalAmount   = order.FinalAmount,
            PaymentMethod = paymentMethod
        };

        switch (paymentMethod)
        {
            case "BankTransfer":
                // Techcombank BIN = 970407 — VietQR quicklink
                const string bankAccountNo = "19071735156018";
                const string bankBin       = "970407"; // Techcombank
                vm.AccountNumber = bankAccountNo;
                vm.AccountName   = "WEARWHATEVER";
                vm.BankName      = "Techcombank (TCB)";
                vm.QrImageUrl    = $"https://img.vietqr.io/image/{bankBin}-{bankAccountNo}-compact2.png" +
                                   $"?amount={amountInt}&addInfo={transferContent}&accountName={Uri.EscapeDataString("WEARWHATEVER")}";
                break;

            case "MoMo":
                const string momoPhone = "0362980422";
                vm.AccountNumber = momoPhone;
                vm.AccountName   = "WEARWHATEVER";
                vm.BankName      = "Ví MoMo";
                // Use QR code generator for MoMo deep-link text
                var momoText = Uri.EscapeDataString($"DH_{order.OrderCode}_{order.ShippingRecipientName}");
                vm.QrImageUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={momoText}";
                break;

            case "ZaloPay":
                const string zaloPhone = "0362980422";
                vm.AccountNumber = zaloPhone;
                vm.AccountName   = "WEARWHATEVER";
                vm.BankName      = "ZaloPay";
                var zaloText = Uri.EscapeDataString($"DH_{order.OrderCode}_{order.ShippingRecipientName}");
                vm.QrImageUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={zaloText}";
                break;
        }

        return vm;
    }

    /// <summary>[LOW-02 FIX] Thread-safe order code generation using timestamp + crypto random.</summary>
    private static string GenerateOrderCode()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var suffix    = Random.Shared.Next(1000, 9999);
        return $"ORD{timestamp}{suffix}";
    }
}
