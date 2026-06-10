using ClothingStore.Models.Entities;
using ClothingStore.Models.ViewModels;

namespace ClothingStore.Repositories;

public interface IProductRepository
{
    Task<List<SearchSuggestionViewModel>> GetSearchSuggestionsAsync(string keyword);
    Task<PagedResult<ProductCardViewModel>> SearchProductsAsync(ProductSearchFilter filter);
    Task<List<Product>> SearchProductsAsync(ProductFilter filter);
    Task<int> CountProductsAsync(ProductFilter filter);
    Task<Product?> GetProductBySlugAsync(string slug);
    Task<Product?> GetProductDetailsAsync(int productId);
    Task<List<Product>> GetRelatedProductsAsync(int productId, int categoryId, int count = 4);
    Task<List<Product>> GetBestSellingProductsAsync(int count = 4);
    Task<List<Product>> GetDynamicBestSellerProductsAsync(int count = 4);
    Task<List<Product>> GetInStockProductsAsync(int count = 4);
    Task<Product?> GetProductForAdminAsync(int productId);
    Task<List<Product>> GetAdminProductsAsync(int page = 1, int pageSize = 20);
    Task<int> CountAdminProductsAsync();
    
    // New Advanced Admin Methods
    Task<(List<Product> Products, int TotalCount)> GetAdminProductsFilteredAsync(ClothingStore.Models.ViewModels.AdminProductFilter filter);
    Task<ClothingStore.Models.ViewModels.ProductDashboardStatsViewModel> GetAdminProductStatsAsync();
    Task<Dictionary<int, int>> GetTotalSoldForProductsAsync(List<int> productIds);
    Task<List<Category>> GetActiveCategoriesAsync();
    Task<List<Category>> GetCategoriesWithChildrenAsync();
    Task<List<DiscountProgram>> GetActiveDiscountProgramsAsync();
    Task<List<Size>> GetActiveSizesAsync();
    Task<List<Color>> GetActiveColorsAsync();
    Task<ProductVariant?> GetVariantAsync(int variantId);
    Task<ProductImage?> GetImageAsync(int imageId);
    Task<int> GetNextImageDisplayOrderAsync(int variantId);
}
