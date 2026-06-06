using System.Security.Claims;

namespace ClothingStore.Extensions;

public static class PermissionExtensions
{
    public static bool HasPermission(this HttpContext context, string requiredPermission)
    {
        if (context.Items["Permissions"] is HashSet<string> permissions)
        {
            if (permissions.Contains("*") || permissions.Contains(requiredPermission, StringComparer.OrdinalIgnoreCase))
                return true;

            var parts = requiredPermission.Split('.');
            if (parts.Length == 2)
            {
                var moduleWildcard = $"{parts[0]}.*";
                if (permissions.Contains(moduleWildcard, StringComparer.OrdinalIgnoreCase))
                    return true;
            }
        }
        return false;
    }
}
