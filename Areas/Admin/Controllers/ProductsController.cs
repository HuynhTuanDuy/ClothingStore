using ClothingStore.Models.ViewModels;
using ClothingStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothingStore.Areas.Admin.Controllers;

[Area("Admin")]
public class ProductsController(IAdminProductService productService) : Controller
{
    public async Task<IActionResult> Index(string status = null)
    {
        var products = await productService.GetProductsAsync();
        
        if (status == "active") 
        {
            products = products.Where(p => p.IsActive).ToList();
        } 
        else if (status == "hidden") 
        {
            products = products.Where(p => !p.IsActive).ToList();
        }
        
        return View(products);
    }

    public async Task<IActionResult> ExportCsv([FromServices] ClothingStore.Data.StoreDbContext context)
    {
        var products = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.Include(context.Products, p => p.Category)
        );

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("ProductID,ProductName,Slug,Category,TotalStock,Status,CreatedAt");

        foreach (var p in products)
        {
            var catName = $"\"{p.Category?.CategoryName?.Replace("\"", "\"\"")}\"";
            var pName = $"\"{p.ProductName?.Replace("\"", "\"\"")}\"";
            var status = p.IsActive ? "Active" : "Hidden";
            var createdAt = p.CreatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
            
            // Note: If you need to count total stock from variants, it would require including Variants.
            // For simplicity, we just export the base product info if variants are not eagerly loaded.
            
            sb.AppendLine($"{p.ProductID},{pName},\"{p.Slug}\",{catName},,{status},{createdAt}");
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "ProductsReport.csv");
    }

    public async Task<IActionResult> Create()
    {
        return View(await productService.CreateProductModelAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await productService.PopulateProductListsAsync(model);
            return View(model);
        }

        var productId = await productService.SaveProductAsync(model);
        return RedirectToAction(nameof(Attributes), new { id = productId });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model = await productService.GetProductEditModelAsync(id);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await productService.PopulateProductListsAsync(model);
            return View(model);
        }

        var productId = await productService.SaveProductAsync(model);
        return productId == 0 ? NotFound() : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await productService.ToggleProductActiveStatusAsync(id);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Attributes(int id)
    {
        if (id == 0)
        {
            var newModel = await productService.CreateProductModelAsync();
            newModel.ProductName = "Sản phẩm mới";
            return View(newModel);
        }
        var model = await productService.GetProductEditModelAsync(id);
        return model is null ? NotFound() : View(model);
    }

    public async Task<IActionResult> Variants(int id)
    {
        if (id == 0) return View(new AdminVariantListViewModel { ProductID = 0, ProductName = "Sản phẩm mới" });
        var model = await productService.GetVariantsAsync(id);
        return model is null ? NotFound() : View(model);
    }

    public async Task<IActionResult> CreateVariant(int productId)
    {
        var model = await productService.CreateVariantModelAsync(productId);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateVariant(VariantEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await productService.PopulateVariantListsAsync(model);
            return View(model);
        }

        var saved = await productService.SaveVariantAsync(model);
        return saved ? RedirectToAction(nameof(Variants), new { id = model.ProductID }) : NotFound();
    }

    public async Task<IActionResult> EditVariant(int id)
    {
        var model = await productService.GetVariantEditModelAsync(id);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditVariant(VariantEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await productService.PopulateVariantListsAsync(model);
            return View(model);
        }

        var saved = await productService.SaveVariantAsync(model);
        return saved ? RedirectToAction(nameof(Variants), new { id = model.ProductID }) : NotFound();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteVariant(int id, int productId)
    {
        await productService.DeactivateVariantAsync(id);
        return RedirectToAction(nameof(Variants), new { id = productId });
    }

    public async Task<IActionResult> Images(int id)
    {
        if (id == 0) return View(new AdminProductImagesViewModel { Upload = new ProductImageUploadViewModel { ProductID = 0, ProductName = "Sản phẩm mới" } });
        var model = await productService.GetImagesAsync(id);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImage(ProductImageUploadViewModel model)
    {
        var error = await productService.UploadImageAsync(model);
        if (error is not null)
        {
            TempData["Error"] = error;
        }

        return RedirectToAction(nameof(Images), new { id = model.ProductID });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int id)
    {
        var result = await productService.DeleteImageAsync(id);
        return result.ProductID.HasValue
            ? RedirectToAction(nameof(Images), new { id = result.ProductID.Value })
            : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetThumbnail(int productId, string imageUrl)
    {
        var success = await productService.SetThumbnailAsync(productId, imageUrl);
        if (!success)
        {
            TempData["Error"] = "Sản phẩm không tồn tại.";
        }
        return RedirectToAction(nameof(Images), new { id = productId });
    }

    /// <summary>Returns all uploaded product images for the image-picker modal.</summary>
    [HttpGet]
    public IActionResult GetLibraryImages()
    {
        var uploadsPath = Path.Combine(
            Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");

        if (!Directory.Exists(uploadsPath))
            return Json(Array.Empty<object>());

        var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var files = Directory.GetFiles(uploadsPath)
            .Where(f => allowedExt.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .OrderByDescending(f => System.IO.File.GetLastWriteTime(f))
            .Select(f => new
            {
                url  = "/uploads/products/" + Path.GetFileName(f),
                name = Path.GetFileName(f),
                size = new FileInfo(f).Length
            })
            .ToArray();

        return Json(files);
    }

    [HttpPost]
    public IActionResult DeleteLibraryImage([FromBody] string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return BadRequest();

        var fileName = Path.GetFileName(url);
        var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");
        var filePath = Path.Combine(uploadsPath, fileName);

        if (System.IO.File.Exists(filePath))
        {
            try
            {
                System.IO.File.Delete(filePath);
                return Ok();
            }
            catch
            {
                return StatusCode(500, "Could not delete file.");
            }
        }
        return NotFound();
    }
}
