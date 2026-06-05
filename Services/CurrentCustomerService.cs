using System.Security.Claims;
using ClothingStore.Models.Entities;

namespace ClothingStore.Services;

/// <summary>
/// [BUG-03 FIX] Reads customer identity from cookie claims instead of returning null.
/// </summary>
public class CurrentCustomerService(IHttpContextAccessor httpContextAccessor) : ICurrentCustomerService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated == true;

    public bool IsAdmin =>
        User?.IsInRole("Admin") == true;

    public int? GetCustomerId()
    {
        var val = User?.FindFirstValue("CustomerId");
        return int.TryParse(val, out var id) ? id : null;
    }

    public int? GetUserId()
    {
        var val = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(val, out var id) ? id : null;
    }

    public string? GetFullName() =>
        User?.FindFirstValue("FullName");
}
