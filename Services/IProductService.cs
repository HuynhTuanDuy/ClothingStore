using ClothingStore.Models.ViewModels;
using ClothingStore.Repositories;

namespace ClothingStore.Services;

public interface IProductService
{
    Task<List<SearchSuggestionViewModel>> GetSearchSuggestionsAsync(string keyword);
    Task<ProductSearchResultViewModel> SearchProductsAsync(ProductSearchFilter filter);
    Task<ProductListViewModel> GetProductListAsync(ProductFilter filter);
    Task<ProductDetailViewModel?> GetProductDetailsAsync(int productId);
    Task<ProductDetailViewModel?> GetProductBySlugAsync(string slug);
    Task<List<ProductCardViewModel>> GetRecommendedProductsAsync(List<CartItemViewModel> cartItems, int count = 4);
    Task<List<ProductCardViewModel>> GetDynamicBestSellingProductsAsync(int count = 4);
}
