using ClothingStore.Models.Entities;

namespace ClothingStore.Repositories;

public interface ICartRepository
{
    Task<Cart?> GetActiveCartByCustomerIdAsync(int customerId);
    Task<Cart?> GetActiveCartBySessionKeyAsync(string sessionKey);
    Task<Cart?> GetCartWithItemsAsync(int cartId);
    Task AddCartAsync(Cart cart);
    Task MergeGuestCartAsync(Cart guestCart, Cart userCart);
}
