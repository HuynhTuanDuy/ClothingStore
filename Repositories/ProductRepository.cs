using ClothingStore.Data;
using ClothingStore.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

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
    int PageSize = 12,
    string? SortBy = null
);

public class ProductRepository(StoreDbContext dbContext, IMemoryCache cache) : IProductRepository
{
    public async Task<List<Product>> SearchProductsAsync(ProductFilter filter)
    {
        if (filter.SortBy?.ToLower() == "bestselling")
        {
            var idQuery = BuildProductQuery(filter, includeDetails: false).Select(x => x.ProductID);
            var filteredIds = await idQuery.ToListAsync();

            var salesDict = await GetProductSalesDictionaryAsync();
            var pageIds = filteredIds
                .OrderByDescending(id => salesDict.GetValueOrDefault(id, 0))
                .ThenByDescending(id => id) // tie-breaker
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            if (pageIds.Count == 0) return new List<Product>();

            var products = await dbContext.Products
                .AsNoTracking()
                .Include(x => x.Category)
                .Include(x => x.DiscountProgram)
                .Include(x => x.ProductVariants.Where(v => v.IsActive))
                    .ThenInclude(v => v.Color)
                .Include(x => x.ProductVariants.Where(v => v.IsActive))
                    .ThenInclude(v => v.Size)
                .Include(x => x.ProductVariants.Where(v => v.IsActive))
                    .ThenInclude(v => v.ProductImages.OrderBy(i => i.DisplayOrder).Take(1))
                .Where(x => pageIds.Contains(x.ProductID))
                .ToListAsync();

            return products.OrderBy(p => pageIds.IndexOf(p.ProductID)).ToList();
        }

        var query = BuildProductQuery(filter, includeDetails: true);
        return await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();
    }

    public Task<int> CountProductsAsync(ProductFilter filter)
    {
        var query = BuildProductQuery(filter, includeDetails: false);
        return query.CountAsync();
    }

    /// <summary>
    /// [HIGH-03 FIX] Full filter: search, category, gender, color, size, price range.
    /// [MED-01 FIX] Include Color and Size to avoid N+1.
    /// </summary>
    private IQueryable<Product> BuildProductQuery(ProductFilter f, bool includeDetails = true)
    {
        var query = dbContext.Products.AsNoTracking();

        if (includeDetails)
        {
            query = query
                .Include(x => x.Category)
                .Include(x => x.DiscountProgram)
                .Include(x => x.ProductVariants.Where(v => v.IsActive))
                    .ThenInclude(v => v.Color)                  // [MED-01 FIX]
                .Include(x => x.ProductVariants.Where(v => v.IsActive))
                    .ThenInclude(v => v.Size)                   // [MED-01 FIX]
                .Include(x => x.ProductVariants.Where(v => v.IsActive))
                    .ThenInclude(v => v.ProductImages.OrderBy(i => i.DisplayOrder).Take(1));
        }

        query = query.Where(x => x.IsActive);

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

        switch (f.SortBy?.ToLower())
        {
            case "priceasc":
                return query.OrderBy(x => x.ProductVariants.Where(v => v.IsActive).Min(v => (decimal?)v.SellingPrice) ?? 0);
            case "pricedesc":
                return query.OrderByDescending(x => x.ProductVariants.Where(v => v.IsActive).Max(v => (decimal?)v.SellingPrice) ?? 0);
            case "nameasc":
                return query.OrderBy(x => x.ProductName);
            case "namedesc":
                return query.OrderByDescending(x => x.ProductName);
            case "bestselling":
                // Handled in memory inside SearchProductsAsync
                return query;
            case "newest":
            default:
                return query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.ProductName);
        }
    }

    private async Task<Dictionary<int, int>> GetProductSalesDictionaryAsync()
    {
        return await cache.GetOrCreateAsync("ProductSalesDictionary", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4);
            var sales = await dbContext.OrderDetails
                .AsNoTracking()
                .Where(od => od.Order.OrderStatus == OrderStatus.Delivered ||
                             od.Order.OrderStatus == OrderStatus.Shipping ||
                             od.Order.OrderStatus == OrderStatus.Processing ||
                             od.Order.OrderStatus == OrderStatus.Confirmed)
                .GroupBy(od => od.ProductVariant.ProductID)
                .Select(g => new { ProductID = g.Key, TotalSold = g.Sum(od => od.Quantity) })
                .ToDictionaryAsync(x => x.ProductID, x => x.TotalSold);
            return sales;
        }) ?? new Dictionary<int, int>();
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

    public async Task<List<Product>> GetDynamicBestSellerProductsAsync(int count = 4)
    {
        return await GetBestSellingProductsAsync(count);
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

    public async Task<(List<Product> Products, int TotalCount)> GetAdminProductsFilteredAsync(ClothingStore.Models.ViewModels.AdminProductFilter filter)
    {
        var query = dbContext.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.DiscountProgram)
            .Include(x => x.ProductVariants)
            .AsQueryable();

        // 1. Filter
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var kw = filter.Search.Trim().ToLower();
            query = query.Where(x => 
                x.ProductName.ToLower().Contains(kw) || 
                x.Slug.ToLower().Contains(kw) || 
                x.ProductVariants.Any(v => v.SKU.ToLower().Contains(kw)));
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(x => x.CategoryID == filter.CategoryId.Value || x.Category.ParentCategoryID == filter.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            if (filter.Status == "active") query = query.Where(x => x.IsActive);
            else if (filter.Status == "inactive") query = query.Where(x => !x.IsActive);
        }

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(x => x.ProductVariants.Any(v => v.SellingPrice >= filter.MinPrice.Value));
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(x => x.ProductVariants.Any(v => v.SellingPrice <= filter.MaxPrice.Value));
        }

        if (!string.IsNullOrWhiteSpace(filter.HasDiscount))
        {
            if (filter.HasDiscount == "yes") query = query.Where(x => x.ProgramID != null && x.DiscountProgram != null && x.DiscountProgram.IsActive);
            else if (filter.HasDiscount == "no") query = query.Where(x => x.ProgramID == null || x.DiscountProgram == null || !x.DiscountProgram.IsActive);
        }

        if (filter.DateFrom.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= filter.DateFrom.Value);
        }

        if (filter.DateTo.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= filter.DateTo.Value.AddDays(1).AddTicks(-1));
        }

        if (!string.IsNullOrWhiteSpace(filter.StockStatus))
        {
            if (filter.StockStatus == "instock") 
                query = query.Where(x => x.ProductVariants.Any(v => v.StockQuantity > 0));
            else if (filter.StockStatus == "outofstock") 
                query = query.Where(x => !x.ProductVariants.Any(v => v.StockQuantity > 0));
            else if (filter.StockStatus == "lowstock") 
                query = query.Where(x => x.ProductVariants.Any(v => v.StockQuantity > 0 && v.StockQuantity <= 5));
        }

        // 2. Count Total BEFORE Pagination
        var totalCount = await query.CountAsync();

        // 3. Sort
        var isDesc = filter.SortDesc;
        switch (filter.SortBy?.ToLower())
        {
            case "name":
                query = isDesc ? query.OrderByDescending(x => x.ProductName) : query.OrderBy(x => x.ProductName);
                break;
            case "price":
                query = isDesc 
                    ? query.OrderByDescending(x => x.ProductVariants.Max(v => (decimal?)v.SellingPrice) ?? 0) 
                    : query.OrderBy(x => x.ProductVariants.Min(v => (decimal?)v.SellingPrice) ?? 0);
                break;
            case "stock":
                query = isDesc 
                    ? query.OrderByDescending(x => x.ProductVariants.Sum(v => v.StockQuantity)) 
                    : query.OrderBy(x => x.ProductVariants.Sum(v => v.StockQuantity));
                break;
            case "sold":
                // This is a bit tricky via LINQ. We will join with OrderDetails.
                // Or simply default to CreatedAt if not easily sorted in DB without large join.
                // Let's do a subquery sum
                query = isDesc
                    ? query.OrderByDescending(x => dbContext.OrderDetails.Where(od => od.ProductVariant.ProductID == x.ProductID && od.Order.OrderStatus != "Cancelled").Sum(od => (int?)od.Quantity) ?? 0)
                    : query.OrderBy(x => dbContext.OrderDetails.Where(od => od.ProductVariant.ProductID == x.ProductID && od.Order.OrderStatus != "Cancelled").Sum(od => (int?)od.Quantity) ?? 0);
                break;
            case "category":
                query = isDesc ? query.OrderByDescending(x => x.Category.CategoryName) : query.OrderBy(x => x.Category.CategoryName);
                break;
            case "status":
                query = isDesc ? query.OrderByDescending(x => x.IsActive) : query.OrderBy(x => x.IsActive);
                break;
            case "updated":
                query = isDesc ? query.OrderByDescending(x => x.UpdatedAt) : query.OrderBy(x => x.UpdatedAt);
                break;
            case "created":
            default:
                query = isDesc ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt);
                break;
        }

        // 4. Paginate
        if (filter.PageSize > 0) // if PageSize is 0, fetch all (for export)
        {
            var page = filter.Page < 1 ? 1 : filter.Page;
            query = query.Skip((page - 1) * filter.PageSize).Take(filter.PageSize);
        }

        var products = await query.ToListAsync();
        return (products, totalCount);
    }

    public async Task<ClothingStore.Models.ViewModels.ProductDashboardStatsViewModel> GetAdminProductStatsAsync()
    {
        var total = await dbContext.Products.CountAsync();
        var active = await dbContext.Products.CountAsync(x => x.IsActive);
        var inactive = total - active;

        // Products with total stock <= 0
        var outOfStock = await dbContext.Products.CountAsync(x => x.IsActive && !x.ProductVariants.Any(v => v.StockQuantity > 0));
        
        // Products with any variant between 1 and 5
        var lowStock = await dbContext.Products.CountAsync(x => x.IsActive && x.ProductVariants.Any(v => v.StockQuantity > 0 && v.StockQuantity <= 5));

        return new ClothingStore.Models.ViewModels.ProductDashboardStatsViewModel
        {
            TotalProducts = total,
            ActiveProducts = active,
            InactiveProducts = inactive,
            OutOfStockProducts = outOfStock,
            LowStockProducts = lowStock
        };
    }

    public async Task<Dictionary<int, int>> GetTotalSoldForProductsAsync(List<int> productIds)
    {
        if (productIds == null || !productIds.Any()) return new Dictionary<int, int>();
        
        var sales = await dbContext.OrderDetails
            .AsNoTracking()
            .Where(od => productIds.Contains(od.ProductVariant.ProductID) && od.Order.OrderStatus != "Cancelled")
            .GroupBy(od => od.ProductVariant.ProductID)
            .Select(g => new { ProductId = g.Key, Sold = g.Sum(od => od.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Sold);

        return sales;
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
