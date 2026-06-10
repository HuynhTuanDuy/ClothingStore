using ClothingStore.Models.ViewModels;
using ClothingStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothingStore.Controllers;

public class ProductsController(IProductService productService, ILogger<ProductsController> logger) : Controller
{
    [HttpGet("Products/Search")]
    public async Task<IActionResult> Search([FromQuery] ProductSearchFilter filter)
    {
        logger.LogInformation("Product search executed. Keyword={Keyword}, Page={Page}, Sort={Sort}", filter.Keyword, filter.Page, filter.Sort);
        var result = await productService.SearchProductsAsync(filter);
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

        logger.LogInformation("Product suggestions executed. Keyword={Keyword}", keyword);
        var result = await productService.GetSearchSuggestionsAsync(keyword);
        return Json(new { items = result });
    }
}
