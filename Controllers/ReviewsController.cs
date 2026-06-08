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
    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ProductReviews([FromQuery] ReviewFilterParams filter)
    {
        if (filter.ProductId <= 0) return BadRequest();

        // Spam protection on paging
        if (filter.Page < 1) filter.Page = 1;
        if (filter.PageSize < 1) filter.PageSize = 10;
        if (filter.PageSize > 50) filter.PageSize = 50;

        // Normalization
        var validSorts = new[] { "newest", "oldest", "highest", "lowest", "mosthelpful" };
        if (!string.IsNullOrEmpty(filter.Sort) && !validSorts.Contains(filter.Sort.ToLowerInvariant()))
        {
            filter.Sort = "newest";
        }

        var validSentiments = new[] { "positive", "neutral", "negative" };
        if (!string.IsNullOrEmpty(filter.Sentiment) && !validSentiments.Contains(filter.Sentiment.ToLowerInvariant()))
        {
            filter.Sentiment = null;
        }

        if (filter.Rating.HasValue && (filter.Rating < 1 || filter.Rating > 5))
        {
            filter.Rating = null;
        }

        filter.CurrentCustomerId = currentCustomerService.GetCustomerId();

        var result = await reviewService.GetProductReviewsAsync(filter);

        ViewBag.TotalCount = result.TotalCount;
        ViewBag.Page = filter.Page;
        ViewBag.PageSize = filter.PageSize;

        return PartialView("_ProductReviews", result.Reviews);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleHelpfulVote([FromForm] int reviewId)
    {
        var customerId = currentCustomerService.GetCustomerId();
        if (!customerId.HasValue)
        {
            return Unauthorized(new { success = false, message = "Vui lòng đăng nhập để thực hiện chức năng này." });
        }

        var result = await reviewService.ToggleHelpfulVoteAsync(reviewId, customerId.Value);

        return Json(new
        {
            success = result.Success,
            message = result.Message,
            isVoted = result.IsVoted,
            helpfulCount = result.HelpfulCount
        });
    }
}
