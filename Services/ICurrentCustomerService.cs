namespace ClothingStore.Services;

public interface ICurrentCustomerService
{
    int? GetCustomerId();
    int? GetUserId();
    string? GetFullName();
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
}
