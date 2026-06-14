using ClothingStore.Models.ViewModels;
using ClothingStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClothingStore.Areas.Shipper.Controllers;

[Area("Shipper")]
[Authorize(Roles = "Shipper")]
[Route("Shipper/[action]")]
public class ShipperController(IShipperService shipperService) : Controller
{
    private int GetCurrentUserId()
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(userIdStr, out var userId);
        return userId;
    }

    public async Task<IActionResult> Dashboard()
    {
        var userId = GetCurrentUserId();
        var vm = await shipperService.GetDashboardAsync(userId);
        return View(vm);
    }

    public async Task<IActionResult> Orders(string? status)
    {
        var userId = GetCurrentUserId();
        var orders = await shipperService.GetOrdersAsync(userId, status);
        ViewBag.CurrentStatus = status;
        return View(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Details(int id)
    {
        var userId = GetCurrentUserId();
        var order = await shipperService.GetOrderDetailAsync(id, userId);
        if (order == null)
        {
            return NotFound("Không tìm thấy đơn hàng hoặc bạn không có quyền xem đơn hàng này.");
        }
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, DeliveryStatusUpdateViewModel model)
    {
        var userId = GetCurrentUserId();
        var result = await shipperService.UpdateDeliveryStatusAsync(id, userId, model);
        
        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
