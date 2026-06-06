using ClothingStore.Models.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ClothingStore.Services;

public class PermissionService(IOptions<SecurityOptions> securityOptions, IMemoryCache cache, ILogger<PermissionService> logger) : IPermissionService
{
    public Task<HashSet<string>> GetUserPermissionsAsync(int userId, IEnumerable<string> roles)
    {
        var cacheKey = $"UserPermissions_{userId}";
        return cache.GetOrCreateAsync(cacheKey, entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(1);

            var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var options = securityOptions.Value;

            foreach (var role in roles)
            {
                logger.LogWarning("Checking role: {Role}, Total configured roles: {Count}", role, options.RolePermissions.Count);
                if (options.RolePermissions.TryGetValue(role, out var roleItems))
                {
                    foreach (var item in roleItems)
                    {
                        // Nếu item là tên của một PermissionGroup, thì add toàn bộ quyền trong group đó
                        if (options.PermissionGroups.TryGetValue(item, out var groupPerms))
                        {
                            foreach (var p in groupPerms)
                            {
                                permissions.Add(p);
                            }
                        }
                        else
                        {
                            // Nếu không thì add trực tiếp (vd: "*", "Dashboard.View")
                            permissions.Add(item);
                        }
                    }
                }
            }

            return Task.FromResult(permissions);
        })!;
    }
}
