using ClothingStore.Data;
using ClothingStore.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClothingStore.Repositories;

/// <summary>Encapsulates all product list filter params.</summary>
public record ProductFilter(
    string? Search = null,
    int? CategoryId = null,
    string? Gender = null,
    int? ColorId = null,
    int? SizeId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int Page = 1,
    int PageSize = 12
);

public class ProductRepository(StoreDbContext dbContext) : IProductRepository
{
    public async Task<List<Product>> SearchProductsAsync(ProductFilter filter)
    {
        var query = BuildProductQuery(filter);
        return await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();
    }

    public Task<int> CountProductsAsync(ProductFilter filter)
    {
        var query = BuildProductQuery(filter);
        return query.CountAsync();
    }

    /// <summary>
    /// [HIGH-03 FIX] Full filter: search, category, gender, color, size, price range.
    /// [MED-01 FIX] Include Color and Size to avoid N+1.
    /// </summary>
    private IQueryable<Product> BuildProductQuery(ProductFilter f)
    {
        var query = dbContext.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.DiscountProgram)
            .Include(x => x.ProductVariants.Where(v => v.IsActive))
                .ThenInclude(v => v.Color)                  // [MED-01 FIX]
            .Include(x => x.ProductVariants.Where(v => v.IsActive))
                .ThenInclude(v => v.Size)                   // [MED-01 FIX]
            .Include(x => x.ProductVariants.Where(v => v.IsActive))
                .ThenInclude(v => v.ProductImages.OrderBy(i => i.DisplayOrder).Take(1))
            .Where(x => x.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var kw = f.Search.Trim();
            query = query.Where(x => x.ProductName.Contains(kw) || (x.Description != null && x.Description.Contains(kw)));
        }

        if (f.CategoryId.HasValue)
            query = query.Where(x => x.CategoryID == f.CategoryId.Value ||
                                     (x.Category.ParentCategoryID == f.CategoryId.Value));

        if (!string.IsNullOrWhiteSpace(f.Gender))
            query = query.Where(x => x.Gender == f.Gender);

        if (f.ColorId.HasValue)
            query = query.Where(x => x.ProductVariants.Any(v => v.IsActive && v.ColorID == f.ColorId.Value));

        if (f.SizeId.HasValue)
            query = query.Where(x => x.ProductVariants.Any(v => v.IsActive && v.SizeID == f.SizeId.Value));

        if (f.MinPrice.HasValue)
            query = query.Where(x => x.ProductVariants.Any(v => v.IsActive && v.SellingPrice >= f.MinPrice.Value));

        if (f.MaxPrice.HasValue)
            query = query.Where(x => x.ProductVariants.Any(v => v.IsActive && v.SellingPrice <= f.MaxPrice.Value));

        return query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.ProductName);
    }

    public Task<Product?> GetProductBySlugAsync(string slug)
    {
        return dbContext.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.DiscountProgram)
            .Include(x => x.ProductVariants.Where(v => v.IsActive))
                .ThenInclude(x => x.Size)
            .Include(x => x.ProductVariants.Where(v => v.IsActive))
                .ThenInclude(x => x.Color)
            .Include(x => x.ProductVariants.Where(v => v.IsActive))
                .ThenInclude(x => x.ProductImages.OrderBy(i => i.DisplayOrder))
            .Include(x => x.Reviews.Where(r => r.IsApproved).OrderByDescending(r => r.ReviewDate).Take(10))
                .ThenInclude(r => r.Customer)
            .FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive);
    }

    public Task<Product?> GetProductDetailsAsync(int productId)
    {
        return dbContext.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.DiscountProgram)
            .Include(x => x.ProductVariants.Where(v => v.IsActive))
                .ThenInclude(x => x.Size)
            .Include(x => x.ProductVariants.Where(v => v.IsActive))
                .ThenInclude(x => x.Color)
            .Include(x => x.ProductVariants.Where(v => v.IsActive))
                .ThenInclude(x => x.ProductImages.OrderBy(i => i.DisplayOrder))
            .Include(x => x.Reviews.Where(r => r.IsApproved).OrderByDescending(r => r.ReviewDate).Take(10))
                .ThenInclude(r => r.Customer)
            .FirstOrDefaultAsync(x => x.ProductID == productId && x.IsActive);
    }

    public async Task<List<Product>> GetRelatedProductsAsync(int productId, int categoryId, int count = 4)
    {
        // 1. Try to get manually selected related products
        var relatedProductIds = await dbContext.ProductRelationships
            .Where(r => r.ProductID == productId)
            .Select(r => r.LinkedProductID)
            .ToListAsync();

        if (relatedProductIds.Any())
        {
            var explicitRelated = await dbContext.Products
                .AsNoTracking()
                .Include(x => x.Category)
                .Include(x => x.DiscountProgram)
                .Include(x => x.ProductVariants.Where(v => v.IsActive))
                    .ThenInclude(v => v.Color)
                .Include(x => x.ProductVariants.Where(v => v.IsActive))
                    .ThenInclude(v => v.ProductImages.OrderBy(i => i.DisplayOrder).Take(1))
                .Where(x => relatedProductIds.Contains(x.ProductID) && x.IsActive)
                .Take(count)
                .ToListAsync();
                
            if (explicitRelated.Any())
                return explicitRelated;
        }

        // Fallback: random products in the same category that have stock
        var randomProducts = await dbContext.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.DiscountProgram)
            .Include(x => x.ProductVariants.Where(v => v.IsActive))
                .ThenInclude(v => v.Color)
            .Include(x => x.ProductVariants.Where(v => v.IsActive))
                .ThenInclude(v => v.ProductImages.OrderBy(i => i.DisplayOrder).Take(1))
            .Where(x => x.ProductID != productId && x.IsActive && x.ProductVariants.Any(v => v.IsActive && v.StockQuantity > 0))
            .OrderBy(x => Guid.NewGuid())
            .Take(count)
            .ToListAsync();
            
        return randomProducts;
    }

    public async Task<List<Product>> GetBestSellingProductsAsync(int count = 4)
    {
        return await dbContext.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.DiscountProgram)
            .Include(x => x.ProductVariants.Where(v => v.IsActive))
                .ThenInclude(v => v.Color)
            .Include(x => x.ProductVariants.Where(v => v.IsActive))
                .ThenInclude(v => v.ProductImages.OrderBy(i => i.DisplayOrder).Take(1))
            .Where(x => x.IsActive && x.IsBestSeller)
            .OrderByDescending(x => x.ProductID)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<Product>> GetInStockProductsAsync(int count = 4)
    {
        return await dbContext.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.DiscountProgram)
            .Include(x => x.ProductVariants.Where(v => v.IsActive))
                .ThenInclude(v => v.Color)
            .Include(x => x.ProductVariants.Where(v => v.IsActive))
                .ThenInclude(v => v.ProductImages.OrderBy(i => i.DisplayOrder).Take(1))
            .Where(x => x.IsActive && x.ProductVariants.Any(v => v.IsActive && v.StockQuantity > 0))
            .OrderBy(x => Guid.NewGuid())
            .Take(count)
            .ToListAsync();
    }

    public Task<Product?> GetProductForAdminAsync(int productId)
    {
        return dbContext.Products
            .Include(x => x.Category)
            .Include(x => x.DiscountProgram)
            .Include(x => x.RelatedProducts)
            .Include(x => x.ProductVariants)
                .ThenInclude(x => x.Size)
            .Include(x => x.ProductVariants)
                .ThenInclude(x => x.Color)
            .Include(x => x.ProductVariants)
                .ThenInclude(x => x.ProductImages.OrderBy(i => i.DisplayOrder))
            .FirstOrDefaultAsync(x => x.ProductID == productId);
    }

    public async Task<List<Product>> GetAdminProductsAsync(int page = 1, int pageSize = 20)
    {
        return await dbContext.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.ProductVariants)
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.ProductName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public Task<int> CountAdminProductsAsync()
    {
        return dbContext.Products.AsNoTracking().CountAsync();
    }

    public async Task<List<Category>> GetActiveCategoriesAsync()
    {
        return await dbContext.Categories
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.CategoryName)
            .ToListAsync();
    }

    public async Task<List<Category>> GetCategoriesWithChildrenAsync()
    {
        return await dbContext.Categories
            .AsNoTracking()
            .Include(x => x.ChildCategories.Where(c => c.IsActive))
            .Where(x => x.IsActive && x.ParentCategoryID == null)
            .OrderBy(x => x.CategoryName)
            .ToListAsync();
    }

    public async Task<List<DiscountProgram>> GetActiveDiscountProgramsAsync()
    {
        return await dbContext.DiscountPrograms
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.ProgramName)
            .ToListAsync();
    }

    public async Task<List<Size>> GetActiveSizesAsync()
    {
        return await dbContext.Sizes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)   // [MED-04 FIX] correct sort order
            .ThenBy(x => x.SizeCode)
            .ToListAsync();
    }

    public async Task<List<Color>> GetActiveColorsAsync()
    {
        return await dbContext.Colors
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.ColorName)
            .ToListAsync();
    }

    public Task<ProductVariant?> GetVariantAsync(int variantId)
    {
        return dbContext.ProductVariants
            .Include(x => x.Product)
                .ThenInclude(x => x.DiscountProgram)
            .Include(x => x.Size)
            .Include(x => x.Color)
            .Include(x => x.ProductImages)
            .FirstOrDefaultAsync(x => x.VariantID == variantId);
    }

    public Task<ProductImage?> GetImageAsync(int imageId)
    {
        return dbContext.ProductImages
            .Include(x => x.ProductVariant)
            .FirstOrDefaultAsync(x => x.ImageID == imageId);
    }

    public async Task<int> GetNextImageDisplayOrderAsync(int variantId)
    {
        var maxOrder = await dbContext.ProductImages
            .Where(x => x.VariantID == variantId)
            .Select(x => (int?)x.DisplayOrder)
            .MaxAsync();
        return (maxOrder ?? 0) + 1;
    }
}
