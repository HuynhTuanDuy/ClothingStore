namespace ClothingStore.Services;

public interface IAddressRecommendationService
{
    Task<int?> GetSuggestedAddressAsync(int customerId);
}
