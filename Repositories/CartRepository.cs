using ClothingStore.Data;
using ClothingStore.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClothingStore.Repositories;

public class CartRepository(StoreDbContext dbContext) : ICartRepository
{
    public Task<Cart?> GetActiveCartByCustomerIdAsync(int customerId)
    {
        return QueryWithItems()
            .FirstOrDefaultAsync(x => x.CustomerId == customerId && x.Status == CartStatus.Active);
    }

    public Task<Cart?> GetActiveCartBySessionKeyAsync(string sessionKey)
    {
        return QueryWithItems()
            .FirstOrDefaultAsync(x => x.SessionKey == sessionKey && x.Status == CartStatus.Active);
    }

    public Task<Cart?> GetCartWithItemsAsync(int cartId)
    {
        return QueryWithItems()
            .FirstOrDefaultAsync(x => x.CartID == cartId);
    }

    public async Task AddCartAsync(Cart cart)
    {
        await dbContext.Carts.AddAsync(cart);
    }

    /// <summary>
    /// [HIGH-07 FIX] Merge guest cart → user cart.
    /// For each guest item: if variant already in user cart → sum quantities (capped at stock);
    /// otherwise move item to user cart. Then abandon guest cart.
    /// </summary>
    public async Task MergeGuestCartAsync(Cart guestCart, Cart userCart)
    {
        foreach (var guestItem in guestCart.CartItems.ToList())
        {
            var existingItem = userCart.CartItems
                .FirstOrDefault(x => x.VariantID == guestItem.VariantID);

            if (existingItem is not null)
            {
                var maxQty = guestItem.ProductVariant.StockQuantity;
                existingItem.Quantity = Math.Min(existingItem.Quantity + guestItem.Quantity, maxQty);
                dbContext.CartItems.Remove(guestItem);
            }
            else
            {
                guestItem.CartID = userCart.CartID;
            }
        }

        guestCart.Status = CartStatus.Abandoned;
        guestCart.SessionKey = null; // Free the unique session index
        guestCart.UpdatedAt = DateTime.UtcNow;
        userCart.UpdatedAt = DateTime.UtcNow;
    }

    private IQueryable<Cart> QueryWithItems()
    {
        return dbContext.Carts
            .Include(x => x.CartItems)
                .ThenInclude(x => x.ProductVariant)
                    .ThenInclude(x => x.Product)
                        .ThenInclude(x => x.DiscountProgram)  // [BUG-05 FIX] load discount
            .Include(x => x.CartItems)
                .ThenInclude(x => x.ProductVariant)
                    .ThenInclude(x => x.Size)
            .Include(x => x.CartItems)
                .ThenInclude(x => x.ProductVariant)
                    .ThenInclude(x => x.Color)
            .Include(x => x.CartItems)
                .ThenInclude(x => x.ProductVariant)
                    .ThenInclude(x => x.ProductImages.OrderBy(i => i.DisplayOrder).Take(1));
    }
}
