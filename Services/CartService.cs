using ClothingStore.Models.Entities;
using ClothingStore.Models.ViewModels;
using ClothingStore.Repositories;

namespace ClothingStore.Services;

public class CartService(
    ICartRepository cartRepository,
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    ICurrentCustomerService currentCustomerService,
    IHttpContextAccessor httpContextAccessor) : ICartService
{
    public const string SessionCartKey = "CartSessionKey";

    public async Task<CartViewModel> GetCartAsync()
    {
        var cart = await GetOrCreateActiveCartAsync();
        return MapCart(cart);
    }

    public async Task<Cart> GetOrCreateActiveCartAsync()
    {
        var customerId = currentCustomerService.GetCustomerId();
        Cart? cart;

        if (customerId.HasValue)
        {
            cart = await cartRepository.GetActiveCartByCustomerIdAsync(customerId.Value);
            if (cart is not null) return cart;

            cart = new Cart
            {
                CustomerId = customerId.Value,
                Status    = CartStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        else
        {
            var session    = httpContextAccessor.HttpContext!.Session;
            var sessionKey = session.GetString(SessionCartKey);
            if (string.IsNullOrWhiteSpace(sessionKey))
            {
                sessionKey = Guid.NewGuid().ToString("N");
                session.SetString(SessionCartKey, sessionKey);
            }

            cart = await cartRepository.GetActiveCartBySessionKeyAsync(sessionKey);
            if (cart is not null) return cart;

            cart = new Cart
            {
                SessionKey = sessionKey,
                Status    = CartStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        await cartRepository.AddCartAsync(cart);
        await unitOfWork.SaveChangesAsync();
        return cart;
    }

    public async Task<string?> AddItemAsync(AddToCartInputModel input)
    {
        if (input.Quantity < 1)
            return "Số lượng phải ít nhất là 1.";

        var variant = await productRepository.GetVariantAsync(input.VariantID);
        if (variant is null || !variant.IsActive || !variant.Product.IsActive)
            return "Sản phẩm này hiện không có sẵn.";

        if (variant.StockQuantity == 0)
            return "Sản phẩm đã hết hàng.";

        var cart     = await GetOrCreateActiveCartAsync();
        var existing = cart.CartItems.FirstOrDefault(x => x.VariantID == input.VariantID);
        var newQty   = input.Quantity + (existing?.Quantity ?? 0);

        if (newQty > variant.StockQuantity)
            return $"Chỉ còn {variant.StockQuantity} sản phẩm ({variant.Size.SizeCode}, {variant.Color.ColorName}) trong kho.";

        if (existing is not null)
        {
            existing.Quantity = newQty;
        }
        else
        {
            cart.CartItems.Add(new CartItem
            {
                CartID    = cart.CartID,
                VariantID = input.VariantID,
                Quantity  = input.Quantity,
                AddedAt   = DateTime.UtcNow
            });
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync();
        return null;
    }

    public async Task<string?> UpdateQuantityAsync(int cartItemId, int quantity)
    {
        var cart = await GetOrCreateActiveCartAsync();
        var item = cart.CartItems.FirstOrDefault(x => x.CartItemID == cartItemId);
        if (item is null) return "Sản phẩm không tồn tại trong giỏ hàng.";

        if (quantity <= 0)
        {
            cart.CartItems.Remove(item);
            cart.UpdatedAt = DateTime.UtcNow;
            await unitOfWork.SaveChangesAsync();
            return null;
        }

        // [HIGH-08 FIX] Null-safe stock check
        var stock = item.ProductVariant?.StockQuantity ?? 0;
        if (quantity > stock)
            return $"Chỉ còn {stock} sản phẩm trong kho.";

        item.Quantity  = quantity;
        cart.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync();
        return null;
    }

    public async Task RemoveItemAsync(int cartItemId)
    {
        var cart = await GetOrCreateActiveCartAsync();
        var item = cart.CartItems.FirstOrDefault(x => x.CartItemID == cartItemId);
        if (item is not null)
        {
            cart.CartItems.Remove(item);
            cart.UpdatedAt = DateTime.UtcNow;
            await unitOfWork.SaveChangesAsync();
        }
    }

    public async Task<int> GetCartItemCountAsync()
    {
        var cart = await GetOrCreateActiveCartAsync();
        return cart.CartItems.Sum(x => x.Quantity);
    }

    private static CartViewModel MapCart(Cart cart)
    {
        return new CartViewModel
        {
            CartID = cart.CartID,
            Items  = cart.CartItems
                .OrderBy(x => x.AddedAt)
                .Select(x =>
                {
                    var variant  = x.ProductVariant;
                    var product  = variant.Product;
                    var imageUrl = variant.ProductImages
                        .OrderBy(i => i.DisplayOrder)
                        .Select(i => i.ImageURL)
                        .FirstOrDefault();

                    // [BUG-05 FIX] Use EffectivePrice (accounts for DiscountProgram)
                    return new CartItemViewModel
                    {
                        CartItemID    = x.CartItemID,
                        VariantID     = variant.VariantID,
                        ProductID     = product.ProductID,
                        ProductName   = product.ProductName,
                        ProductSlug   = product.Slug,
                        ThumbnailUrl  = imageUrl ?? product.ThumbnailUrl,
                        SizeCode      = variant.Size.SizeCode,
                        ColorName     = variant.Color.ColorName,
                        HexCode       = variant.Color.HexCode,
                        SKU           = variant.SKU,
                        OriginalPrice = variant.SellingPrice,
                        UnitPrice     = variant.EffectivePrice,    // discounted
                        HasDiscount   = variant.HasDiscount,
                        Quantity      = x.Quantity,
                        StockQuantity = variant.StockQuantity
                    };
                })
                .ToList()
        };
    }
}
