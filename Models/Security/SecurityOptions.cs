namespace ClothingStore.Models.Security;

public class SecurityOptions
{
    public Dictionary<string, List<string>> PermissionGroups { get; set; } = new();
    public Dictionary<string, List<string>> RolePermissions { get; set; } = new();
}
