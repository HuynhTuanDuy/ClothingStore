using ClothingStore.Models.ViewModels;
using ClothingStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClothingStore.Controllers;

[Authorize]
public class ReviewsController(IReviewService reviewService, ICurrentCustomerService currentCustomerService) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateReviewViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
            return RedirectToAction("Details", "Home", new { id = model.ProductID });
        }

        var customerId = currentCustomerService.GetCustomerId();
        if (!customerId.HasValue)
        {
            return Forbid();
        }

        var result = await reviewService.CreateReviewAsync(customerId.Value, model);
        
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction("Details", "Home", new { id = model.ProductID });
    }
}
