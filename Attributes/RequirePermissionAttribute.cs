using ClothingStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace ClothingStore.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class RequirePermissionAttribute : TypeFilterAttribute
{
    public RequirePermissionAttribute(string permission) : base(typeof(RequirePermissionFilter))
    {
        Arguments = new object[] { permission };
    }
}


public class RequirePermissionFilter : IAsyncAuthorizationFilter
{
    private readonly string _requiredPermission;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<RequirePermissionFilter> _logger;

    public RequirePermissionFilter(string requiredPermission, IPermissionService permissionService, ILogger<RequirePermissionFilter> logger)
    {
        _requiredPermission = requiredPermission;
        _permissionService = permissionService;
        _logger = logger;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Find the most specific attribute (Action first, then Controller)
        var endpointMetadata = context.ActionDescriptor.EndpointMetadata;
        var requiredPermAttr = endpointMetadata.OfType<RequirePermissionAttribute>().LastOrDefault();
        
        string permissionToCheck = _requiredPermission;
        
        if (requiredPermAttr != null)
        {
            if (requiredPermAttr.Arguments?.FirstOrDefault() is string explicitPerm)
            {
                permissionToCheck = explicitPerm;
            }
        }

        var user = context.HttpContext.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
        {
            context.Result = new ChallengeResult();
            return;
        }

        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value);
        
        var permissions = await _permissionService.GetUserPermissionsAsync(userId, roles);

        // Check exact match or super admin wildcard
        if (permissions.Contains("*") || permissions.Contains(permissionToCheck))
        {
            return;
        }

        // Check module wildcard (e.g. "Product.*" matches "Product.Manage")
        var parts = permissionToCheck.Split('.');
        if (parts.Length == 2)
        {
            var moduleWildcard = $"{parts[0]}.*";
            if (permissions.Contains(moduleWildcard))
            {
                return;
            }
        }

        _logger.LogWarning("AUDIT: User {UserId} ({UserName}) denied access to {Permission}", userId, user.Identity.Name, permissionToCheck);
        context.Result = new ForbidResult();
    }
}
