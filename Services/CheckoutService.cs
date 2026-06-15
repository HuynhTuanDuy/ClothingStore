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
    ICustomerAccountService customerAccountService,
    IHttpContextAccessor httpContextAccessor,
    IMemoryCache memoryCache,
    IAddressService addressService,
    IAddressRecommendationService addressRecommendationService,
    ILogger<CheckoutService> logger) : ICheckoutService
{
    // Shipping fee policy
    private const decimal FreeShippingThreshold = 500_000m;
    private const decimal StandardShippingFee   = 30_000m;

    public async Task<CheckoutViewModel> GetCheckoutAsync(CheckoutInputModel? input = null)
    {
        var vm = new CheckoutViewModel
        {
            Input = input ?? new CheckoutInputModel(),
            Cart  = await cartService.GetCartAsync(),
            Provinces = (await addressService.GetProvincesAsync()).ToList()
        };

        var customerId = currentCustomerService.GetCustomerId();
        if (customerId.HasValue)
        {
            vm.IsAuthenticated = true;
            vm.SavedAddresses = await customerAccountService.GetAddressesAsync(customerId.Value);

            try
            {
                vm.SuggestedAddressId = await addressRecommendationService.GetSuggestedAddressAsync(customerId.Value);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get suggested address for customer {CustomerId}", customerId.Value);
                vm.SuggestedAddressId = null;
            }

            // If it's a new request and no input was provided, try to pre-fill from default address
            if (input == null && vm.SavedAddresses.Any())
            {
                // Filter out legacy (unnormalized) addresses and try to find a default one
                var validAddresses = vm.SavedAddresses.Where(a => a.ProvinceId != null).ToList();
                if (validAddresses.Any())
                {
                    var defaultAddr = validAddresses.FirstOrDefault(a => a.IsDefault) ?? validAddresses.First();
                vm.Input.ShippingRecipientName = defaultAddr.RecipientName;
                vm.Input.ShippingPhone = defaultAddr.ReceiverPhone;
                vm.Input.ShippingAddress = defaultAddr.AddressLine;
                vm.Input.ProvinceId = defaultAddr.ProvinceId;
                vm.Input.DistrictId = defaultAddr.DistrictId;
                vm.Input.WardId = defaultAddr.WardId;
                vm.Input.DeliveryNote = defaultAddr.Note;
                vm.Input.ShippingAddressId = defaultAddr.AddressID;
                
                var profile = await customerAccountService.GetProfileAsync(customerId.Value);
                if (profile != null)
                {
                    vm.Input.OrderEmail = profile.Email;
                }
                }
                else
                {
                    var profile = await customerAccountService.GetProfileAsync(customerId.Value);
                    if (profile != null)
                    {
                        vm.Input.ShippingRecipientName = profile.FullName;
                        vm.Input.ShippingPhone = profile.Phone;
                        vm.Input.OrderEmail = profile.Email;
                    }
                }
            }
            else if (input == null)
            {
                var profile = await customerAccountService.GetProfileAsync(customerId.Value);
                if (profile != null)
                {
                    vm.Input.ShippingRecipientName = profile.FullName;
                    vm.Input.ShippingPhone = profile.Phone;
                    vm.Input.OrderEmail = profile.Email;
                }
            }
        }

        return vm;
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
        var vat = Math.Max(0m, (subTotal - discountAmount) * 0.1m);
        var finalAmount = subTotal + shipping - discountAmount + vat;

        // ── Address Validation & Snapshot ────────────────────────────
        if (input.ProvinceId == null || input.DistrictId == null || input.WardId == null)
            return PlaceOrderResult.Failure("Vui lòng chọn đầy đủ Tỉnh, Quận, Phường.");
            
        var province = await addressService.GetProvinceByIdAsync(input.ProvinceId.Value);
        var district = await addressService.GetDistrictByIdAsync(input.DistrictId.Value);
        var ward = await addressService.GetWardByIdAsync(input.WardId.Value);
        
        if (province == null || district == null || ward == null)
            return PlaceOrderResult.Failure("Địa chỉ giao hàng không còn được hỗ trợ. Vui lòng chọn lại địa chỉ.");
            
        if (district.ProvinceId != province.ProvinceId || ward.DistrictId != district.DistrictId)
            return PlaceOrderResult.Failure("Dữ liệu phường/quận/tỉnh không khớp nhau.");
            
        var addrParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(input.ShippingAddress))
            addrParts.Add(input.ShippingAddress.Trim());
        addrParts.Add(ward.Name);
        addrParts.Add(district.Name);
        addrParts.Add(province.Name);

        string fullAddress = string.Join(", ", addrParts);
        if (!string.IsNullOrWhiteSpace(input.DeliveryNote))
            fullAddress += $". Ghi chú: {input.DeliveryNote.Trim()}";

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
            ShippingWard         = ward.Name,
            ShippingDistrict     = district.Name,
            ShippingProvince     = province.Name,
            ShippingFullAddress  = fullAddress,
            ShippingAddressId    = input.ShippingAddressId,
            DeliveryNote         = input.DeliveryNote?.Trim(),
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
