using ClothingStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClothingStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Employee")]
public class ReviewsController(IReviewService reviewService) : Controller
{
    public async Task<IActionResult> Index(int page = 1)
    {
        var model = await reviewService.GetAdminReviewsAsync(page, 20);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var success = await reviewService.ApproveReviewAsync(id);
        if (success)
            TempData["SuccessMessage"] = "Đã duyệt đánh giá thành công.";
        else
            TempData["ErrorMessage"] = "Không tìm thấy đánh giá.";
            
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await reviewService.DeleteReviewAsync(id);
        if (success)
            TempData["SuccessMessage"] = "Đã xóa đánh giá thành công.";
        else
            TempData["ErrorMessage"] = "Không tìm thấy đánh giá.";
            
        return RedirectToAction(nameof(Index));
    }
}
