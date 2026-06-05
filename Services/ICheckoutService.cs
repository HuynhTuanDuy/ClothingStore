using ClothingStore.Models.ViewModels;

namespace ClothingStore.Services;

public interface ICheckoutService
{
    Task<CheckoutViewModel> GetCheckoutAsync(CheckoutInputModel? input = null);
    Task<PlaceOrderResult> PlaceOrderAsync(CheckoutInputModel input);
    Task<OrderSuccessViewModel?> GetOrderSuccessAsync(string orderCode);
    Task<CouponApplyResult> ApplyCouponAsync(string couponCode, decimal subTotal, int? customerId);
    Task<PaymentPendingViewModel?> GetPaymentPendingAsync(string orderCode, string paymentMethod);
}
