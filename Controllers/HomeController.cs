using System.Diagnostics;
using ClothingStore.Models;
using ClothingStore.Models.ViewModels;
using ClothingStore.Repositories;
using ClothingStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothingStore.Controllers;

public class HomeController(IProductService productService, ICurrentCustomerService currentCustomerService, IReviewService reviewService) : Controller
{
    public async Task<IActionResult> Index(
        string? search = null,
        int? categoryId = null,
        string? gender = null,
        int? colorId = null,
        int? sizeId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? sortBy = null,
        int page = 1)
    {
        var filter = new ProductFilter(search, categoryId, gender, colorId, sizeId, minPrice, maxPrice, page, 4, sortBy);
        var productListModel = await productService.GetProductListAsync(filter);
        productListModel.SortBy = sortBy;

        var bestSellersList = await productService.GetDynamicBestSellingProductsAsync(4); 

        var model = new HomeViewModel
        {
            ShopProducts = productListModel,
            BestSellers = bestSellersList
        };

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
        string? sortBy = null,
        int page = 1)
    {
        var filter = new ProductFilter(search, categoryId, gender, colorId, sizeId, minPrice, maxPrice, page, 12, sortBy);
        var model  = await productService.GetProductListAsync(filter);
        model.SortBy = sortBy;
        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var model = await productService.GetProductDetailsAsync(id);
        if (model != null)
        {
            var customerId = currentCustomerService.GetCustomerId();
            if (customerId.HasValue)
            {
                model.CanReview = await reviewService.CanUserReviewProductAsync(customerId.Value, id);
            }
        }
        return model is null ? NotFound() : View(model);
    }

    [HttpGet("Products/Details/{slug}")]
    public async Task<IActionResult> ProductBySlug(string slug)
    {
        var model = await productService.GetProductBySlugAsync(slug);
        if (model != null)
        {
            var customerId = currentCustomerService.GetCustomerId();
            if (customerId.HasValue)
            {
                model.CanReview = await reviewService.CanUserReviewProductAsync(customerId.Value, model.ProductID);
            }
        }
        return model is null ? NotFound() : View("Details", model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
