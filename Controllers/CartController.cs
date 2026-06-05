using ClothingStore.Models.ViewModels;
using ClothingStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothingStore.Controllers;

public class CartController(ICartService cartService, IProductService productService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var cart = await cartService.GetCartAsync();
        cart.RecommendedProducts = await productService.GetRecommendedProductsAsync(cart.Items, 4);
        return View(cart);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(AddToCartInputModel input)
    {
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
                     (Request.ContentType?.Contains("application/json") == true);

        if (!ModelState.IsValid)
        {
            if (isAjax) return Json(new { success = false, message = "Please choose a valid variant and quantity." });
            TempData["Error"] = "Please choose a valid variant and quantity.";
            return RedirectToAction("Index", "Home");
        }

        var error = await cartService.AddItemAsync(input);
        if (error is not null)
        {
            if (isAjax) return Json(new { success = false, message = error });
            TempData["Error"] = error;
            return RedirectToAction("Details", "Home", new { id = Request.Form["ProductID"].ToString() });
        }

        if (isAjax)
        {
            var totalItems = await cartService.GetCartItemCountAsync();
            return Json(new { success = true, message = "Item added to cart.", totalItems = totalItems });
        }

        TempData["Success"] = "Item added to cart.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int cartItemId, int quantity)
    {
        var error = await cartService.UpdateQuantityAsync(cartItemId, quantity);
        if (error is not null)
        {
            TempData["Error"] = error;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int cartItemId)
    {
        await cartService.RemoveItemAsync(cartItemId);
        return RedirectToAction(nameof(Index));
    }
}
