namespace ClothingStore.Services;

public interface IPermissionService
{
    Task<HashSet<string>> GetUserPermissionsAsync(int userId, IEnumerable<string> roles);
}
