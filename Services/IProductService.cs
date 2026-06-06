using ClothingStore.Models.ViewModels;
using ClothingStore.Repositories;

namespace ClothingStore.Services;

public interface IProductService
{
    Task<ProductListViewModel> GetProductListAsync(ProductFilter filter);
    Task<ProductDetailViewModel?> GetProductDetailsAsync(int productId);
    Task<ProductDetailViewModel?> GetProductBySlugAsync(string slug);
    Task<List<ProductCardViewModel>> GetRecommendedProductsAsync(List<CartItemViewModel> cartItems, int count = 4);
    Task<List<ProductCardViewModel>> GetDynamicBestSellingProductsAsync(int count = 4);
}
