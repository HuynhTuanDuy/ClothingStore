using ClothingStore.Models.ViewModels;
using ClothingStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothingStore.Areas.Admin.Controllers;

[Area("Admin")]
public class ProductsController(IAdminProductService productService) : Controller
{
    public async Task<IActionResult> Index([FromQuery] AdminProductFilter filter)
    {
        var model = await productService.GetProductsFilteredAsync(filter);
        ViewBag.ParentCategories = (await (HttpContext.RequestServices.GetRequiredService<ClothingStore.Repositories.IProductRepository>()).GetActiveCategoriesAsync())
            .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = c.CategoryID.ToString(), Text = c.CategoryName })
            .ToList();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkAction(string actionType, List<int> productIds, int? categoryId)
    {
        if (productIds == null || !productIds.Any()) return RedirectToAction(nameof(Index));

        switch (actionType)
        {
            case "activate":
                await productService.BulkUpdateStatusAsync(productIds, true);
                TempData["Success"] = $"Đã kích hoạt {productIds.Count} sản phẩm.";
                break;
            case "deactivate":
                await productService.BulkUpdateStatusAsync(productIds, false);
                TempData["Success"] = $"Đã vô hiệu hóa {productIds.Count} sản phẩm.";
                break;
            case "delete":
                await productService.BulkDeleteAsync(productIds);
                TempData["Success"] = $"Đã xóa (mềm) {productIds.Count} sản phẩm.";
                break;
            case "changeCategory":
                if (categoryId.HasValue)
                {
                    await productService.BulkUpdateCategoryAsync(productIds, categoryId.Value);
                    TempData["Success"] = $"Đã cập nhật danh mục cho {productIds.Count} sản phẩm.";
                }
                break;
        }

        // Keep current url params if possible, but simplest is redirect to index.
        // We will just return to Index. A better UX would use AJAX.
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duplicate(int id)
    {
        var newId = await productService.DuplicateProductAsync(id);
        if (newId > 0)
        {
            TempData["Success"] = "Đã nhân bản sản phẩm thành công.";
            return RedirectToAction(nameof(Edit), new { id = newId });
        }
        TempData["Error"] = "Lỗi nhân bản sản phẩm.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ExportCsv([FromQuery] AdminProductFilter filter)
    {
        // Ignore pagination for export
        filter.PageSize = 0; 
        var model = await productService.GetProductsFilteredAsync(filter);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("ProductID,SKU,ProductName,Category,MinPrice,MaxPrice,TotalStock,TotalSold,Status,CreatedAt");

        foreach (var p in model.Products)
        {
            var pName = $"\"{p.ProductName.Replace("\"", "\"\"")}\"";
            var status = p.IsActive ? "Active" : "Hidden";
            var createdAt = p.CreatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
            
            sb.AppendLine($"{p.ProductID},{p.SKU},{pName},\"{p.CategoryName}\",{p.MinPrice},{p.MaxPrice},{p.TotalStock},{p.TotalSold},{status},{createdAt}");
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "ProductsReport.csv");
    }

    public async Task<IActionResult> ExportExcel([FromQuery] AdminProductFilter filter)
    {
        filter.PageSize = 0; 
        var model = await productService.GetProductsFilteredAsync(filter);

        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Products");
        
        // Headers
        worksheet.Cell(1, 1).Value = "ProductID";
        worksheet.Cell(1, 2).Value = "SKU";
        worksheet.Cell(1, 3).Value = "ProductName";
        worksheet.Cell(1, 4).Value = "Category";
        worksheet.Cell(1, 5).Value = "MinPrice";
        worksheet.Cell(1, 6).Value = "MaxPrice";
        worksheet.Cell(1, 7).Value = "TotalStock";
        worksheet.Cell(1, 8).Value = "TotalSold";
        worksheet.Cell(1, 9).Value = "Status";
        worksheet.Cell(1, 10).Value = "CreatedAt";

        // Data
        var row = 2;
        foreach (var p in model.Products)
        {
            worksheet.Cell(row, 1).Value = p.ProductID;
            worksheet.Cell(row, 2).Value = p.SKU;
            worksheet.Cell(row, 3).Value = p.ProductName;
            worksheet.Cell(row, 4).Value = p.CategoryName;
            worksheet.Cell(row, 5).Value = p.MinPrice;
            worksheet.Cell(row, 6).Value = p.MaxPrice;
            worksheet.Cell(row, 7).Value = p.TotalStock;
            worksheet.Cell(row, 8).Value = p.TotalSold;
            worksheet.Cell(row, 9).Value = p.IsActive ? "Active" : "Hidden";
            worksheet.Cell(row, 10).Value = p.CreatedAt?.ToString("yyyy-MM-dd HH:mm:ss");
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new System.IO.MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ProductsReport.xlsx");
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
