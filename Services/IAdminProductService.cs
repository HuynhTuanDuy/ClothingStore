using ClothingStore.Models.ViewModels;

namespace ClothingStore.Services;

public interface IAdminProductService
{
    Task<IReadOnlyList<AdminProductListItemViewModel>> GetProductsAsync();
    Task<AdminProductPageViewModel> GetProductsPagedAsync(int page = 1, int pageSize = 20);
    Task<ProductEditViewModel> CreateProductModelAsync();
    Task<ProductEditViewModel?> GetProductEditModelAsync(int productId);
    Task PopulateProductListsAsync(ProductEditViewModel model);
    Task<int> SaveProductAsync(ProductEditViewModel model);
    Task<bool> ToggleProductActiveStatusAsync(int productId);
    Task<AdminVariantListViewModel?> GetVariantsAsync(int productId);
    Task<VariantEditViewModel?> CreateVariantModelAsync(int productId);
    Task<VariantEditViewModel?> GetVariantEditModelAsync(int variantId);
    Task PopulateVariantListsAsync(VariantEditViewModel model);
    Task<bool> SaveVariantAsync(VariantEditViewModel model);
    Task<bool> DeactivateVariantAsync(int variantId);
    Task<AdminProductImagesViewModel?> GetImagesAsync(int productId);
    Task<string?> UploadImageAsync(ProductImageUploadViewModel model);
    Task<(bool Succeeded, int? ProductID)> DeleteImageAsync(int imageId);
    Task<bool> SetThumbnailAsync(int productId, string imageUrl);
}
