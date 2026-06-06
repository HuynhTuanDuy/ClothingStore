using ClothingStore.Models.Entities;
using ClothingStore.Models.ViewModels;
using ClothingStore.Repositories;

namespace ClothingStore.Services;

public class ProductService(IProductRepository productRepository) : IProductService
{
    public async Task<ProductListViewModel> GetProductListAsync(ProductFilter filter)
    {
        var products   = await productRepository.SearchProductsAsync(filter);
        var total      = await productRepository.CountProductsAsync(filter);
        var categories = await productRepository.GetActiveCategoriesAsync();
        var sizes      = await productRepository.GetActiveSizesAsync();
        var colors     = await productRepository.GetActiveColorsAsync();

        return new ProductListViewModel
        {
            Search     = filter.Search,
            CategoryID = filter.CategoryId,
            Gender     = filter.Gender,
            ColorId    = filter.ColorId,
            SizeId     = filter.SizeId,
            MinPrice   = filter.MinPrice,
            MaxPrice   = filter.MaxPrice,
            Page       = filter.Page,
            PageSize   = filter.PageSize,
            TotalCount = total,
            Categories = categories.Select(x => new CategoryFilterViewModel
            {
                CategoryID   = x.CategoryID,
                CategoryName = x.CategoryName
            }).ToList(),
            Sizes  = sizes.Select(x => new SizeFilterViewModel  { SizeID = x.SizeID, SizeCode = x.SizeCode }).ToList(),
            Colors = colors.Select(x => new ColorFilterViewModel { ColorID = x.ColorID, ColorName = x.ColorName, HexCode = x.HexCode }).ToList(),
            Products = products.Select(MapCard).ToList()
        };
    }

    public async Task<ProductDetailViewModel?> GetProductDetailsAsync(int productId)
    {
        var product = await productRepository.GetProductDetailsAsync(productId);
        if (product is null) return null;
        var vm = MapDetails(product);
        var related = await productRepository.GetRelatedProductsAsync(product.ProductID, product.CategoryID, 12);
        var bestSelling = await productRepository.GetDynamicBestSellerProductsAsync(12);
        var inStock = await productRepository.GetInStockProductsAsync(12);
        vm.RelatedProducts = related.Select(MapCard).ToList();
        vm.BestSellingProducts = bestSelling.Select(MapCard).ToList();
        vm.InStockProducts = inStock.Select(MapCard).ToList();
        return vm;
    }

    public async Task<ProductDetailViewModel?> GetProductBySlugAsync(string slug)
    {
        var product = await productRepository.GetProductBySlugAsync(slug);
        if (product is null) return null;
        var vm = MapDetails(product);
        var related = await productRepository.GetRelatedProductsAsync(product.ProductID, product.CategoryID, 12);
        var bestSelling = await productRepository.GetDynamicBestSellerProductsAsync(12);
        var inStock = await productRepository.GetInStockProductsAsync(12);
        vm.RelatedProducts = related.Select(MapCard).ToList();
        vm.BestSellingProducts = bestSelling.Select(MapCard).ToList();
        vm.InStockProducts = inStock.Select(MapCard).ToList();
        return vm;
    }

    public async Task<List<ProductCardViewModel>> GetRecommendedProductsAsync(List<CartItemViewModel> cartItems, int count = 4)
    {
        var firstItem = cartItems?.FirstOrDefault();
        if (firstItem != null)
        {
            var product = await productRepository.GetProductDetailsAsync(firstItem.ProductID);
            if (product != null)
            {
                var related = await productRepository.GetRelatedProductsAsync(product.ProductID, product.CategoryID, count);
                if (related.Any())
                {
                    return related.Select(MapCard).ToList();
                }
            }
        }

        var products = await productRepository.GetInStockProductsAsync(count);
        return products.Select(MapCard).ToList();
    }

    public async Task<List<ProductCardViewModel>> GetDynamicBestSellingProductsAsync(int count = 4)
    {
        var products = await productRepository.GetDynamicBestSellerProductsAsync(count);
        return products.Select(MapCard).ToList();
    }

    /// <summary>
    /// Ensures image URL always starts with '/' for root-relative serving.
    /// Paths like "somi-trang.jpg" become "/somi-trang.jpg".
    /// Absolute URLs (http/https) and already-rooted paths are returned unchanged.
    /// </summary>
    private static string? NormalizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("/"))
            return url;
        return "/" + url;
    }

    /// <summary>
    /// [MED-09 FIX] Show effective (discounted) price on product cards.
    /// </summary>
    private static ProductCardViewModel MapCard(Product product)
    {
        var variants = product.ProductVariants.Where(x => x.IsActive).ToList();

        // Get the first variant image (per-variant images)
        var thumbnailFromVariant = variants
            .SelectMany(v => v.ProductImages)
            .OrderBy(i => i.DisplayOrder)
            .Select(i => NormalizeUrl(i.ImageURL))
            .FirstOrDefault(u => !string.IsNullOrWhiteSpace(u));

        // Effective prices after discount
        var effectivePrices = variants.Select(v => v.EffectivePrice).ToList();

        // Extract distinct colors for the swatches
        var distinctColors = variants
            .Select(v => v.Color)
            .Where(c => c != null)
            .GroupBy(c => c.ColorID)
            .Select(g => g.First())
            .Select(c => new ColorFilterViewModel
            {
                ColorID = c.ColorID,
                ColorName = c.ColorName,
                HexCode = c.HexCode
            }).ToList();

        return new ProductCardViewModel
        {
            ProductID    = product.ProductID,
            ProductName  = product.ProductName,
            ProductSlug  = product.Slug,
            ThumbnailUrl = thumbnailFromVariant ?? NormalizeUrl(product.ThumbnailUrl),
            CategoryName = product.Category?.CategoryName ?? string.Empty,
            OriginalMinPrice = variants.Any() ? variants.Min(v => v.SellingPrice) : 0,
            MinPrice         = effectivePrices.Any() ? effectivePrices.Min() : 0,
            MaxPrice         = effectivePrices.Any() ? effectivePrices.Max() : 0,
            HasDiscount      = product.DiscountProgram != null && product.DiscountProgram.IsCurrentlyActive && product.DiscountProgram.EndDate > DateTime.UtcNow,
            HasStock         = variants.Sum(v => v.StockQuantity) > 0,
            DiscountPercent  = product.DiscountProgram?.DiscountPercent ?? 0,
            IsBestSeller     = product.IsBestSeller,
            Colors           = distinctColors
        };
    }

    /// <summary>
    /// [HIGH-04 FIX] Group images by VariantID so JS can switch images when color is selected.
    /// </summary>
    private static ProductDetailViewModel MapDetails(Product product)
    {
        var variantVms = product.ProductVariants
            .Where(x => x.IsActive)
            .OrderBy(x => x.Color.ColorName)
            .ThenBy(x => x.Size.SortOrder)
            .ThenBy(x => x.Size.SizeCode)
            .Select(x => new ProductVariantViewModel
            {
                VariantID       = x.VariantID,
                SKU             = x.SKU,
                SizeCode        = x.Size.SizeCode,
                ColorName       = x.Color.ColorName,
                HexCode         = x.Color.HexCode,
                SellingPrice    = x.SellingPrice,
                EffectivePrice  = x.EffectivePrice,
                HasDiscount     = x.HasDiscount,
                StockQuantity   = x.StockQuantity,
                IsActive        = x.IsActive,
                // Images grouped by variant for JS switching
                ImageUrls = x.ProductImages
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => NormalizeUrl(i.ImageURL))
                    .Where(u => !string.IsNullOrWhiteSpace(u))
                    .Select(u => u!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
            })
            .ToList();

        // All unique image urls (fallback for gallery)
        var allImageUrls = variantVms
            .SelectMany(v => v.ImageUrls)
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!string.IsNullOrWhiteSpace(product.ThumbnailUrl))
        {
            var normalizedThumb = NormalizeUrl(product.ThumbnailUrl);
            if (normalizedThumb != null && !allImageUrls.Contains(normalizedThumb, StringComparer.OrdinalIgnoreCase))
                allImageUrls.Insert(0, normalizedThumb);
        }

        return new ProductDetailViewModel
        {
            ProductID        = product.ProductID,
            ProductName      = product.ProductName,
            ProductSlug      = product.Slug,
            ThumbnailUrl     = product.ThumbnailUrl,
            Description      = product.Description,
            Gender           = product.Gender,
            Material         = product.Material,
            FitType          = product.FitType,
            CareInstructions = product.CareInstructions,
            CategoryName     = product.Category.CategoryName,
            CategoryID       = product.CategoryID,
            ImageUrls        = allImageUrls,
            Variants         = variantVms,
            Reviews = product.Reviews
                .Where(r => r.IsApproved)
                .OrderByDescending(r => r.ReviewDate)
                .Select(r => new ReviewViewModel
                {
                    ReviewID     = r.ReviewID,
                    Rating       = r.Rating,
                    Comment      = r.Comment,
                    ReviewDate   = r.ReviewDate,
                    CustomerName = r.Customer.FullName
                })
                .ToList()
        };
    }
}
