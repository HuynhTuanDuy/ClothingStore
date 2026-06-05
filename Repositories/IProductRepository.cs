using ClothingStore.Models.Entities;

namespace ClothingStore.Repositories;

public interface IProductRepository
{
    Task<List<Product>> SearchProductsAsync(ProductFilter filter);
    Task<int> CountProductsAsync(ProductFilter filter);
    Task<Product?> GetProductBySlugAsync(string slug);
    Task<Product?> GetProductDetailsAsync(int productId);
    Task<List<Product>> GetRelatedProductsAsync(int productId, int categoryId, int count = 4);
    Task<List<Product>> GetBestSellingProductsAsync(int count = 4);
    Task<List<Product>> GetInStockProductsAsync(int count = 4);
    Task<Product?> GetProductForAdminAsync(int productId);
    Task<List<Product>> GetAdminProductsAsync(int page = 1, int pageSize = 20);
    Task<int> CountAdminProductsAsync();
    Task<List<Category>> GetActiveCategoriesAsync();
    Task<List<Category>> GetCategoriesWithChildrenAsync();
    Task<List<DiscountProgram>> GetActiveDiscountProgramsAsync();
    Task<List<Size>> GetActiveSizesAsync();
    Task<List<Color>> GetActiveColorsAsync();
    Task<ProductVariant?> GetVariantAsync(int variantId);
    Task<ProductImage?> GetImageAsync(int imageId);
    Task<int> GetNextImageDisplayOrderAsync(int variantId);
}
