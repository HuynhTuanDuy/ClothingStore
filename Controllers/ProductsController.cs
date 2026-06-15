using ClothingStore.Models.ViewModels;
using ClothingStore.Services;
using ClothingStore.Data;
using ClothingStore.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;

namespace ClothingStore.Controllers;

public class ProductsController(
    IProductService productService, 
    ILogger<ProductsController> logger,
    StoreDbContext dbContext,
    IMemoryCache cache,
    ICurrentCustomerService currentCustomerService) : Controller
{
    [HttpGet("Products/Search")]
    public async Task<IActionResult> Search([FromQuery] ProductSearchFilter filter)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        filter.CustomerId = currentCustomerService.GetCustomerId();
        var result = await productService.SearchProductsAsync(filter);
        
        sw.Stop();
        
        if (!string.IsNullOrWhiteSpace(filter.Keyword) && filter.Keyword.Trim().Length >= 2)
        {
            var keyword = filter.Keyword.Trim();
            var normalizedKw = ClothingStore.Helpers.VietnameseStringHelper.NormalizeVietnamese(keyword).ToLower();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = Request.Headers.UserAgent.ToString().ToLower();
            
            bool isBotAgent = userAgent.Contains("bot") || userAgent.Contains("crawler") || userAgent.Contains("spider");
            bool isSuspiciousBotKeyword = !keyword.Contains(' ') && System.Text.RegularExpressions.Regex.IsMatch(keyword, @"^[a-z0-9_\-\.]{1,20}$", System.Text.RegularExpressions.RegexOptions.IgnoreCase) && (keyword.Contains('.') || keyword.Contains('_') || keyword.Contains("admin"));

            if (!isBotAgent && !isSuspiciousBotKeyword)
            {
                var cacheKey = $"SearchLog_{(filter.CustomerId?.ToString() ?? ip)}_{normalizedKw}";
                
                if (!cache.TryGetValue(cacheKey, out _))
                {
                    var log = new SearchLog
                    {
                        Keyword = keyword,
                        NormalizedKeyword = normalizedKw,
                        ResultCount = result.Products.TotalCount,
                        ElapsedMilliseconds = sw.ElapsedMilliseconds,
                        SearchedAt = DateTime.UtcNow
                    };
                    
                    dbContext.SearchLogs.Add(log);
                    await dbContext.SaveChangesAsync();
                    
                    // Add to ViewBag so frontend can use it for Click Tracking
                    ViewBag.SearchLogId = log.SearchLogId;
                    
                    cache.Set(cacheKey, true, TimeSpan.FromMinutes(30));
                }
            }
        }

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
    
    [HttpPost("Products/LogSearchClick")]
    public async Task<IActionResult> LogSearchClick([FromForm] int searchLogId, [FromForm] int productId)
    {
        if (searchLogId <= 0 || productId <= 0)
        {
            return BadRequest();
        }

        var log = await dbContext.SearchLogs.FindAsync(searchLogId);
        if (log == null)
        {
            return NotFound();
        }

        // Security check: Only update if not already clicked, or within reasonable time frame (e.g., 2 hours)
        if (log.ClickedProductId == null)
        {
            if ((DateTime.UtcNow - log.SearchedAt).TotalHours <= 2)
            {
                log.ClickedProductId = productId;
                log.ClickedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
            }
        }

        return Ok();
    }

    [HttpGet("Products/Trending")]
    public async Task<IActionResult> Trending()
    {
        var cacheKey = "TrendingSearches";
        if (!cache.TryGetValue(cacheKey, out List<string>? trendingKeywords))
        {
            var cutoff = DateTime.UtcNow.AddDays(-7);
            var rawTrending = await dbContext.SearchLogs
                .Where(x => x.SearchedAt >= cutoff && x.ResultCount > 0 && x.Keyword != null)
                .GroupBy(x => x.Keyword)
                .Select(g => new { 
                    Keyword = g.Key, 
                    SearchCount = g.Count(), 
                    ClickCount = g.Count(x => x.ClickedProductId != null) 
                })
                .OrderByDescending(x => x.SearchCount)
                .Take(50)
                .ToListAsync();

            trendingKeywords = rawTrending
                .Select(x => new { x.Keyword, Ctr = x.SearchCount > 0 ? (double)x.ClickCount / x.SearchCount : 0 })
                .Where(x => x.Ctr > 0)
                .OrderByDescending(x => x.Ctr)
                .ThenByDescending(x => x.Keyword)
                .Take(5)
                .Select(x => x.Keyword)
                .ToList();
                
            cache.Set(cacheKey, trendingKeywords, TimeSpan.FromHours(1));
        }
        
        return Json(new { items = trendingKeywords ?? new List<string>() });
    }
}
