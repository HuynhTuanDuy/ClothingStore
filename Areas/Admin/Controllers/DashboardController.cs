using ClothingStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothingStore.Areas.Admin.Controllers;

[Area("Admin")]
public class DashboardController(IDashboardService dashboardService) : Controller
{
    public async Task<IActionResult> Index(int? year)
    {
        return View(await dashboardService.GetDashboardAsync(year));
    }
}
