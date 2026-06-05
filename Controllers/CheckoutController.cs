using ClothingStore.Models.ViewModels;
using ClothingStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothingStore.Controllers;

public class CheckoutController(ICheckoutService checkoutService) : Controller
{
    public async Task<IActionResult> Index([FromQuery] bool guest = false)
    {
        if (User.Identity?.IsAuthenticated != true && !guest)
        {
            return View("LoginOrGuest");
        }

        var model = await checkoutService.GetCheckoutAsync();
        if (model.Cart.IsEmpty)
        {
            TempData["Error"] = "Your cart is empty.";
            return RedirectToAction("Index", "Cart");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(CheckoutInputModel input)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", await checkoutService.GetCheckoutAsync(input));
        }

        var result = await checkoutService.PlaceOrderAsync(input);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Could not place order.");
            return View("Index", await checkoutService.GetCheckoutAsync(input));
        }

        // For online payment methods — redirect to payment info page
        var pm = input.PaymentMethod;
        if (pm is "BankTransfer" or "MoMo" or "ZaloPay")
        {
            return RedirectToAction(nameof(PaymentPending), new { orderCode = result.OrderCode, paymentMethod = pm });
        }

        return RedirectToAction(nameof(Success), new { orderCode = result.OrderCode });
    }

    public async Task<IActionResult> Success(string orderCode)
    {
        if (string.IsNullOrWhiteSpace(orderCode))
        {
            return NotFound();
        }

        var model = await checkoutService.GetOrderSuccessAsync(orderCode);
        return model is null ? NotFound() : View(model);
    }

    public async Task<IActionResult> PaymentPending(string orderCode, string paymentMethod)
    {
        if (string.IsNullOrWhiteSpace(orderCode))
            return NotFound();

        var model = await checkoutService.GetPaymentPendingAsync(orderCode, paymentMethod);
        return model is null ? NotFound() : View(model);
    }
}
