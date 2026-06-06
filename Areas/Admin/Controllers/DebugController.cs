using ClothingStore.Attributes;
using ClothingStore.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClothingStore.Areas.Admin.Controllers;

[Area("Admin")]
[RequirePermission("*")] // Only Super Admin can access debug endpoints
public class DebugController(IPermissionService permissionService) : Controller
{
    [HttpGet("Admin/Debug/Permissions")]
    public async Task<IActionResult> Permissions()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
            return BadRequest("Invalid User ID");

        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var permissions = await permissionService.GetUserPermissionsAsync(userId, roles);

        return Json(new
        {
            userId = userId,
            roles = roles,
            permissions = permissions.OrderBy(p => p).ToList()
        });
    }
}
