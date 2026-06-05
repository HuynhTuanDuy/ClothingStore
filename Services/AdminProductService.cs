using ClothingStore.Models.Entities;
using ClothingStore.Models.ViewModels;
using ClothingStore.Repositories;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClothingStore.Services;

public class AdminProductService(
    IProductRepository productRepository,
    IRepository<Product> products,
    IRepository<ProductVariant> variants,
    IRepository<ProductImage> images,
    IRepository<ProductRelationship> productRelationships,
    IUnitOfWork unitOfWork,
    IWebHostEnvironment environment) : IAdminProductService
{
    public async Task<IReadOnlyList<AdminProductListItemViewModel>> GetProductsAsync()
    {
        var productList = await productRepository.GetAdminProductsAsync();

        return productList.Select(MapListItem).ToList();
    }

    public async Task<AdminProductPageViewModel> GetProductsPagedAsync(int page = 1, int pageSize = 20)
    {
        var productList = await productRepository.GetAdminProductsAsync(page, pageSize);
        var total       = await productRepository.CountAdminProductsAsync();

        return new AdminProductPageViewModel
        {
            Products   = productList.Select(MapListItem).ToList(),
            Filter     = new AdminProductFilter { Page = page, PageSize = pageSize },
            TotalCount = total
        };
    }

    private static AdminProductListItemViewModel MapListItem(Product x) => new()
    {
        ProductID    = x.ProductID,
        ProductName  = x.ProductName,
        SKU          = x.ProductVariants.FirstOrDefault()?.SKU ?? "",
        CategoryName = x.Category?.CategoryName ?? "",
        ThumbnailUrl = x.ThumbnailUrl,
        VariantCount = x.ProductVariants?.Count ?? 0,
        TotalStock   = x.ProductVariants?.Sum(v => v.StockQuantity) ?? 0,
        MinPrice     = x.ProductVariants?.Any() == true ? x.ProductVariants.Min(v => v.SellingPrice) : 0,
        MaxPrice     = x.ProductVariants?.Any() == true ? x.ProductVariants.Max(v => v.SellingPrice) : 0,
        IsActive     = x.IsActive,
        CreatedAt    = x.CreatedAt,
        UpdatedAt    = x.UpdatedAt
    };

    public async Task<ProductEditViewModel> CreateProductModelAsync()
    {
        var model = new ProductEditViewModel
        {
            IsActive = true
        };
        await PopulateProductListsAsync(model);
        return model;
    }

    public async Task<ProductEditViewModel?> GetProductEditModelAsync(int productId)
    {
        var product = await productRepository.GetProductForAdminAsync(productId);
        if (product is null)
        {
            return null;
        }

        var model = new ProductEditViewModel
        {
            ProductID = product.ProductID,
            ProductName = product.ProductName,
            Slug = product.Slug,
            ThumbnailUrl = product.ThumbnailUrl,
            Description = product.Description,
            Gender = product.Gender,
            Material = product.Material,
            FitType = product.FitType,
            CareInstructions = product.CareInstructions,
            CategoryID = product.CategoryID,
            ProgramID = product.ProgramID,
            IsActive = product.IsActive,
            IsBestSeller = product.IsBestSeller,
            SelectedRelatedProductIds = product.RelatedProducts.Select(r => r.LinkedProductID).ToList()
        };

        await PopulateProductListsAsync(model);
        return model;
    }

    public async Task PopulateProductListsAsync(ProductEditViewModel model)
    {
        var categories = await productRepository.GetActiveCategoriesAsync();
        var programs = await productRepository.GetActiveDiscountProgramsAsync();
        var sizes = await productRepository.GetActiveSizesAsync();
        var colors = await productRepository.GetActiveColorsAsync();

        model.AvailableSizes = sizes.Select(x => new SizeFilterViewModel
        {
            SizeID = x.SizeID,
            SizeCode = x.SizeCode
        }).ToList();

        model.AvailableColors = colors.Select(x => new ColorFilterViewModel
        {
            ColorID = x.ColorID,
            ColorName = x.ColorName,
            HexCode = x.HexCode
        }).ToList();

        model.Categories = categories.Select(x => new SelectListItem
        {
            Value = x.CategoryID.ToString(),
            Text = x.CategoryName,
            Selected = x.CategoryID == model.CategoryID
        }).ToList();

        model.DiscountPrograms = new[] { new SelectListItem { Value = "", Text = "No discount" } }
            .Concat(programs.Select(x => new SelectListItem
            {
                Value = x.ProgramID.ToString(),
                Text = $"{x.ProgramName} ({x.DiscountPercent}%)",
                Selected = x.ProgramID == model.ProgramID
            }))
            .ToList();

        var allProducts = await productRepository.GetAdminProductsAsync(1, 1000); // Assuming small catalog, otherwise need autocomplete endpoint
        model.AvailableProducts = allProducts
            .Where(x => x.ProductID != model.ProductID)
            .Select(x => new SelectListItem
            {
                Value = x.ProductID.ToString(),
                Text = x.ProductName,
                Selected = model.SelectedRelatedProductIds?.Contains(x.ProductID) == true
            }).ToList();
    }

    public async Task<int> SaveProductAsync(ProductEditViewModel model)
    {
        var now = DateTime.UtcNow;
        model.Slug = string.IsNullOrWhiteSpace(model.Slug) ? Slugify(model.ProductName) : Slugify(model.Slug);

        if (model.ProductID == 0)
        {
            var product = new Product
            {
                ProductName = model.ProductName.Trim(),
                Slug = model.Slug,
                ThumbnailUrl = model.ThumbnailUrl,
                Description = model.Description,
                Gender = model.Gender.Trim(),
                Material = model.Material.Trim(),
                FitType = model.FitType.Trim(),
                CareInstructions = model.CareInstructions.Trim(),
                CategoryID = model.CategoryID,
                ProgramID = model.ProgramID,
                IsActive = model.IsActive,
                IsBestSeller = model.IsBestSeller,
                CreatedAt = now,
                UpdatedAt = now
            };

            await products.AddAsync(product);
            await unitOfWork.SaveChangesAsync();

            await SyncRelatedProductsAsync(product.ProductID, model.SelectedRelatedProductIds);

            return product.ProductID;
        }

        var existing = await products.FindAsync(model.ProductID);
        if (existing is null)
        {
            return 0;
        }

        existing.ProductName = model.ProductName.Trim();
        existing.Slug = model.Slug;
        existing.ThumbnailUrl = model.ThumbnailUrl;
        existing.Description = model.Description;
        existing.Gender = model.Gender.Trim();
        existing.Material = model.Material.Trim();
        existing.FitType = model.FitType.Trim();
        existing.CareInstructions = model.CareInstructions.Trim();
        existing.CategoryID = model.CategoryID;
        existing.ProgramID = model.ProgramID;
        existing.IsActive = model.IsActive;
        existing.IsBestSeller = model.IsBestSeller;
        existing.UpdatedAt = now;

        await unitOfWork.SaveChangesAsync();

        await SyncRelatedProductsAsync(existing.ProductID, model.SelectedRelatedProductIds);

        return existing.ProductID;
    }

    private async Task SyncRelatedProductsAsync(int productId, List<int>? selectedIds)
    {
        selectedIds ??= new List<int>();
        var existingRels = await productRelationships.ListAsync(r => r.ProductID == productId);
        foreach (var rel in existingRels)
        {
            productRelationships.Remove(rel);
        }
        
        foreach (var id in selectedIds.Distinct().Where(id => id != productId)) // prevent self linking
        {
            await productRelationships.AddAsync(new ProductRelationship { ProductID = productId, LinkedProductID = id });
        }
        
        await unitOfWork.SaveChangesAsync();
    }

    public async Task<bool> ToggleProductActiveStatusAsync(int productId)
    {
        var product = await products.FindAsync(productId);
        if (product is null)
        {
            return false;
        }

        product.IsActive = !product.IsActive;
        product.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<AdminVariantListViewModel?> GetVariantsAsync(int productId)
    {
        var product = await productRepository.GetProductForAdminAsync(productId);
        if (product is null)
        {
            return null;
        }

        return new AdminVariantListViewModel
        {
            ProductID = product.ProductID,
            ProductName = product.ProductName,
            Variants = product.ProductVariants
                .OrderBy(x => x.Color.ColorName)
                .ThenBy(x => x.Size.SizeCode)
                .Select(x => new ProductVariantViewModel
                {
                    VariantID = x.VariantID,
                    SKU = x.SKU,
                    SizeCode = x.Size.SizeCode,
                    ColorName = x.Color.ColorName,
                    HexCode = x.Color.HexCode,
                    SellingPrice = x.SellingPrice,
                    StockQuantity = x.StockQuantity,
                    IsActive = x.IsActive
                })
                .ToList()
        };
    }

    public async Task<VariantEditViewModel?> CreateVariantModelAsync(int productId)
    {
        var product = await productRepository.GetProductForAdminAsync(productId);
        if (product is null)
        {
            return null;
        }

        var model = new VariantEditViewModel
        {
            ProductID = product.ProductID,
            ProductName = product.ProductName,
            IsActive = true
        };

        await PopulateVariantListsAsync(model);
        return model;
    }

    public async Task<VariantEditViewModel?> GetVariantEditModelAsync(int variantId)
    {
        var variant = await productRepository.GetVariantAsync(variantId);
        if (variant is null)
        {
            return null;
        }

        var model = new VariantEditViewModel
        {
            VariantID = variant.VariantID,
            ProductID = variant.ProductID,
            ProductName = variant.Product.ProductName,
            SKU = variant.SKU,
            SellingPrice = variant.SellingPrice,
            StockQuantity = variant.StockQuantity,
            SizeID = variant.SizeID,
            ColorID = variant.ColorID,
            IsActive = variant.IsActive
        };

        await PopulateVariantListsAsync(model);
        return model;
    }

    public async Task PopulateVariantListsAsync(VariantEditViewModel model)
    {
        var sizes = await productRepository.GetActiveSizesAsync();
        var colors = await productRepository.GetActiveColorsAsync();

        model.Sizes = sizes.Select(x => new SelectListItem
        {
            Value = x.SizeID.ToString(),
            Text = x.SizeCode,
            Selected = x.SizeID == model.SizeID
        }).ToList();

        model.Colors = colors.Select(x => new SelectListItem
        {
            Value = x.ColorID.ToString(),
            Text = x.ColorName,
            Selected = x.ColorID == model.ColorID
        }).ToList();
    }

    public async Task<bool> SaveVariantAsync(VariantEditViewModel model)
    {
        var now = DateTime.UtcNow;

        if (model.VariantID == 0)
        {
            var product = await productRepository.GetProductForAdminAsync(model.ProductID);
            if (product is null)
            {
                return false;
            }

            var variant = new ProductVariant
            {
                ProductID = model.ProductID,
                SKU = model.SKU.Trim(),
                SellingPrice = model.SellingPrice,
                StockQuantity = model.StockQuantity,
                SizeID = model.SizeID,
                ColorID = model.ColorID,
                IsActive = model.IsActive,
                CreatedAt = now,
                UpdatedAt = now
            };

            await variants.AddAsync(variant);
            await unitOfWork.SaveChangesAsync();
            return true;
        }

        var existing = await variants.FindAsync(model.VariantID);
        if (existing is null)
        {
            return false;
        }

        existing.SKU = model.SKU.Trim();
        existing.SellingPrice = model.SellingPrice;
        existing.StockQuantity = model.StockQuantity;
        existing.SizeID = model.SizeID;
        existing.ColorID = model.ColorID;
        existing.IsActive = model.IsActive;
        existing.UpdatedAt = now;

        await unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeactivateVariantAsync(int variantId)
    {
        var variant = await variants.FindAsync(variantId);
        if (variant is null)
        {
            return false;
        }

        variant.IsActive = false;
        variant.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<AdminProductImagesViewModel?> GetImagesAsync(int productId)
    {
        var product = await productRepository.GetProductForAdminAsync(productId);
        if (product is null)
        {
            return null;
        }

        var variantOptions = product.ProductVariants
            .Where(x => x.IsActive)
            .OrderBy(x => x.Color.ColorName)
            .ThenBy(x => x.Size.SizeCode)
            .Select(x => new SelectListItem
            {
                Value = x.VariantID.ToString(),
                Text = $"{x.SKU} - {x.Size.SizeCode} / {x.Color.ColorName}"
            })
            .ToList();

        return new AdminProductImagesViewModel
        {
            Upload = new ProductImageUploadViewModel
            {
                ProductID = product.ProductID,
                ProductName = product.ProductName,
                Variants = variantOptions
            },
            Images = product.ProductVariants
                .SelectMany(v => v.ProductImages.Select(i => new ProductImageListItemViewModel
                {
                    ImageID = i.ImageID,
                    VariantID = v.VariantID,
                    VariantName = $"{v.SKU} - {v.Size.SizeCode} / {v.Color.ColorName}",
                    DisplayOrder = i.DisplayOrder,
                    ImageURL = i.ImageURL,
                    IsMain = product.ThumbnailUrl == i.ImageURL
                }))
                .OrderBy(x => x.VariantName)
                .ThenBy(x => x.DisplayOrder)
                .ToList(),
            ProductThumbnailUrl = product.ThumbnailUrl
        };
    }

    public async Task<string?> UploadImageAsync(ProductImageUploadViewModel model)
    {
        if (model.ImageFiles is null || model.ImageFiles.Count == 0)
        {
            return "Please choose an image file.";
        }

        var variant = await productRepository.GetVariantAsync(model.VariantID);
        if (variant is null || variant.ProductID != model.ProductID)
        {
            return "Selected variant was not found for this product.";
        }

        var allowedExtensions = new HashSet<string> { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var uploadRoot = Path.Combine(environment.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(uploadRoot);

        var displayOrder = model.DisplayOrder > 0 && model.DisplayOrder >= variant.ProductImages.Count + 1
            ? model.DisplayOrder
            : (variant.ProductImages.Count > 0 ? variant.ProductImages.Max(x => x.DisplayOrder) + 1 : 1);

        foreach (var file in model.ImageFiles)
        {
            if (file.Length == 0) continue;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return "Only JPG, PNG, WEBP, and GIF images are supported.";
            }

            var origFileName = Path.GetFileNameWithoutExtension(file.FileName);
            var safeFileName = Slugify(origFileName);
            if (string.IsNullOrWhiteSpace(safeFileName)) safeFileName = Guid.NewGuid().ToString("N");

            var fileName = $"{safeFileName}{extension}";
            var absolutePath = Path.Combine(uploadRoot, fileName);

            if (File.Exists(absolutePath))
            {
                fileName = $"{safeFileName}-{Guid.NewGuid():N}{extension}";
                absolutePath = Path.Combine(uploadRoot, fileName);
            }

            await using (var stream = File.Create(absolutePath))
            {
                await file.CopyToAsync(stream);
            }

            var imageUrl = $"/uploads/products/{fileName}";
            if (!variant.ProductImages.Any(x => string.Equals(x.ImageURL, imageUrl, StringComparison.OrdinalIgnoreCase)))
            {
                await images.AddAsync(new ProductImage
                {
                    VariantID = model.VariantID,
                    DisplayOrder = displayOrder++,
                    ImageURL = imageUrl
                });
            }
        }

        await unitOfWork.SaveChangesAsync();
        return null;
    }

    public async Task<(bool Succeeded, int? ProductID)> DeleteImageAsync(int imageId)
    {
        var image = await productRepository.GetImageAsync(imageId);
        if (image is null)
        {
            return (false, null);
        }

        var productId = image.ProductVariant.ProductID;
        images.Remove(image);
        await unitOfWork.SaveChangesAsync();

        if (image.ImageURL.StartsWith("/uploads/products/", StringComparison.OrdinalIgnoreCase))
        {
            var fileName = image.ImageURL["/uploads/products/".Length..];
            var absolutePath = Path.Combine(environment.WebRootPath, "uploads", "products", fileName);
            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }
        }

        return (true, productId);
    }

    /// <summary>
    /// [HIGH-09 FIX] Properly handles Vietnamese diacritics by normalizing Unicode
    /// then stripping combining characters before slugifying.
    /// e.g. "Áo Thun Nam" → "ao-thun-nam"
    /// </summary>
    private static string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Guid.NewGuid().ToString("N")[..12];

        // Normalize to decomposed form so diacritics become separate chars
        var normalized = value.Trim().ToLowerInvariant()
            .Normalize(System.Text.NormalizationForm.FormD);

        // Keep only ASCII letters, digits; map everything else to '-'
        var chars = normalized
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                        != System.Globalization.UnicodeCategory.NonSpacingMark)
            .Select(c => char.IsAsciiLetterOrDigit(c) ? c : '-')
            .ToArray();

        var slug = string.Join('-', new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("N")[..12] : slug;
    }

    public async Task<bool> SetThumbnailAsync(int productId, string imageUrl)
    {
        var product = await products.FindAsync(productId);
        if (product == null) return false;

        product.ThumbnailUrl = imageUrl;
        products.Update(product);
        await unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<AdminProductPageViewModel> GetProductsFilteredAsync(AdminProductFilter filter)
    {
        var (productList, total) = await productRepository.GetAdminProductsFilteredAsync(filter);
        var stats = await productRepository.GetAdminProductStatsAsync();

        var productIds = productList.Select(x => x.ProductID).ToList();
        var salesDict = await productRepository.GetTotalSoldForProductsAsync(productIds);

        var viewModels = productList.Select(x => {
            var minP = x.ProductVariants.Any() ? x.ProductVariants.Min(v => v.SellingPrice) : 0;
            var maxP = x.ProductVariants.Any() ? x.ProductVariants.Max(v => v.SellingPrice) : 0;
            var totalStock = x.ProductVariants.Sum(v => v.StockQuantity);
            var totalSold = salesDict.GetValueOrDefault(x.ProductID, 0);

            return new AdminProductListItemViewModel
            {
                ProductID    = x.ProductID,
                ProductName  = x.ProductName,
                SKU          = x.ProductVariants.FirstOrDefault()?.SKU ?? "",
                CategoryName = x.Category?.CategoryName ?? "",
                ThumbnailUrl = x.ThumbnailUrl,
                VariantCount = x.ProductVariants?.Count ?? 0,
                TotalStock   = totalStock,
                MinPrice     = minP,
                MaxPrice     = maxP,
                TotalSold    = totalSold,
                IsActive     = x.IsActive,
                CreatedAt    = x.CreatedAt,
                UpdatedAt    = x.UpdatedAt
            };
        }).ToList();

        return new AdminProductPageViewModel
        {
            Products   = viewModels,
            Filter     = filter,
            Stats      = stats,
            TotalCount = total
        };
    }

    public async Task<int> DuplicateProductAsync(int productId)
    {
        var original = await productRepository.GetProductForAdminAsync(productId);
        if (original == null) return 0;

        var now = DateTime.UtcNow;
        var copySlug = $"{original.Slug}-copy-{Guid.NewGuid().ToString("N")[..6]}";

        var copy = new Product
        {
            ProductName = $"{original.ProductName} (Copy)",
            Slug = copySlug,
            ThumbnailUrl = original.ThumbnailUrl,
            Description = original.Description,
            Gender = original.Gender,
            Material = original.Material,
            FitType = original.FitType,
            CareInstructions = original.CareInstructions,
            CategoryID = original.CategoryID,
            ProgramID = original.ProgramID,
            IsActive = false, // Automatically hidden
            IsBestSeller = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        await products.AddAsync(copy);
        await unitOfWork.SaveChangesAsync(); // save to get ID

        // Duplicate variants and images
        foreach (var v in original.ProductVariants)
        {
            var vCopy = new ProductVariant
            {
                ProductID = copy.ProductID,
                SKU = $"{v.SKU}-COPY",
                SellingPrice = v.SellingPrice,
                StockQuantity = v.StockQuantity,
                SizeID = v.SizeID,
                ColorID = v.ColorID,
                IsActive = v.IsActive,
                CreatedAt = now,
                UpdatedAt = now
            };
            await variants.AddAsync(vCopy);
            await unitOfWork.SaveChangesAsync(); // save to get variant ID

            foreach (var img in v.ProductImages)
            {
                await images.AddAsync(new ProductImage
                {
                    VariantID = vCopy.VariantID,
                    DisplayOrder = img.DisplayOrder,
                    ImageURL = img.ImageURL
                });
            }
        }

        await unitOfWork.SaveChangesAsync();
        return copy.ProductID;
    }

    public async Task<(int SuccessCount, int FailCount)> BulkUpdateStatusAsync(List<int> productIds, bool isActive)
    {
        int success = 0;
        foreach (var id in productIds)
        {
            var p = await products.FindAsync(id);
            if (p != null)
            {
                p.IsActive = isActive;
                p.UpdatedAt = DateTime.UtcNow;
                products.Update(p);
                success++;
            }
        }
        await unitOfWork.SaveChangesAsync();
        return (success, productIds.Count - success);
    }

    public async Task<(int SuccessCount, int FailCount)> BulkDeleteAsync(List<int> productIds)
    {
        return await BulkUpdateStatusAsync(productIds, false);
    }

    public async Task<(int SuccessCount, int FailCount)> BulkUpdateCategoryAsync(List<int> productIds, int newCategoryId)
    {
        int success = 0;
        foreach (var id in productIds)
        {
            var p = await products.FindAsync(id);
            if (p != null)
            {
                p.CategoryID = newCategoryId;
                p.UpdatedAt = DateTime.UtcNow;
                products.Update(p);
                success++;
            }
        }
        await unitOfWork.SaveChangesAsync();
        return (success, productIds.Count - success);
    }
}
