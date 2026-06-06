using System.Security.Claims;
using ClothingStore.Services;

namespace ClothingStore.Middleware;

public class PermissionMiddleware
{
    private readonly RequestDelegate _next;

    public PermissionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IPermissionService permissionService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out var userId))
            {
                var roles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value);
                var permissions = await permissionService.GetUserPermissionsAsync(userId, roles);
                context.Items["Permissions"] = permissions;
            }
        }

        await _next(context);
    }
}
