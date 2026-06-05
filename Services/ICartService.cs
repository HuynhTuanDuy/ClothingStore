using ClothingStore.Models.ViewModels;

namespace ClothingStore.Services;

public interface ICartService
{
    Task<CartViewModel> GetCartAsync();
    Task<Models.Entities.Cart> GetOrCreateActiveCartAsync();
    Task<string?> AddItemAsync(AddToCartInputModel input);
    Task<string?> UpdateQuantityAsync(int cartItemId, int quantity);
    Task RemoveItemAsync(int cartItemId);
    Task<int> GetCartItemCountAsync();
}
