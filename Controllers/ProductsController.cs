using ClothingStore.Models.ViewModels;
using ClothingStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothingStore.Controllers;

public class ProductsController(IProductService productService, ILogger<ProductsController> logger) : Controller
{
    [HttpGet("Products/Search")]
    public async Task<IActionResult> Search([FromQuery] ProductSearchFilter filter)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await productService.SearchProductsAsync(filter);
        sw.Stop();
        logger.LogInformation("Product search executed in {ElapsedMilliseconds}ms. Keyword={Keyword}, Page={Page}, Sort={Sort}", sw.ElapsedMilliseconds, filter.Keyword, filter.Page, filter.Sort);
        return View(result);
    }

    [HttpGet("Products/Suggestions")]
    public async Task<IActionResult> Suggestions([FromQuery] string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return Json(new { items = Array.Empty<object>() });
        }

        keyword = keyword.Trim();
        if (keyword.Length < 2)
        {
            return Json(new { items = Array.Empty<object>() });
        }

        if (keyword.Length > 100)
        {
            keyword = keyword.Substring(0, 100);
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await productService.GetSearchSuggestionsAsync(keyword);
        sw.Stop();
        logger.LogInformation("Product suggestions executed in {ElapsedMilliseconds}ms. Keyword={Keyword}", sw.ElapsedMilliseconds, keyword);
        return Json(new { items = result });
    }
}
