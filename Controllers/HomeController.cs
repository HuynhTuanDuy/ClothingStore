using System.Diagnostics;
using ClothingStore.Models;
using ClothingStore.Repositories;
using ClothingStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothingStore.Controllers;

public class HomeController(IProductService productService) : Controller
{
    public async Task<IActionResult> Index(
        string? search = null,
        int? categoryId = null,
        string? gender = null,
        int? colorId = null,
        int? sizeId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        int page = 1)
    {
        var filter = new ProductFilter(search, categoryId, gender, colorId, sizeId, minPrice, maxPrice, page) with { PageSize = 4 };
        var model  = await productService.GetProductListAsync(filter);
        return View(model);
    }

    public async Task<IActionResult> ShopAll(
        string? search = null,
        int? categoryId = null,
        string? gender = null,
        int? colorId = null,
        int? sizeId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        int page = 1)
    {
        var filter = new ProductFilter(search, categoryId, gender, colorId, sizeId, minPrice, maxPrice, page);
        var model  = await productService.GetProductListAsync(filter);
        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var model = await productService.GetProductDetailsAsync(id);
        return model is null ? NotFound() : View(model);
    }

    public async Task<IActionResult> ProductBySlug(string slug)
    {
        var model = await productService.GetProductBySlugAsync(slug);
        return model is null ? NotFound() : View("Details", model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
