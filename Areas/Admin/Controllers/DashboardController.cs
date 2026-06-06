using ClothingStore.Services;
using Microsoft.AspNetCore.Mvc;


using ClothingStore.Attributes;

namespace ClothingStore.Areas.Admin.Controllers;

[Area("Admin")]
[RequirePermission("Dashboard.View")]
public class DashboardController(IDashboardService dashboardService) : Controller
{
    public async Task<IActionResult> Index(int? year)
    {
        return View(await dashboardService.GetDashboardAsync(year));
    }
}
