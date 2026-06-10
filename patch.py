import os

file_path = r'c:\Code\TMDT\WEB_USER_TMDT\ClothingStore\Repositories\ProductRepository.cs'
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Replace GetSearchSuggestionsAsync
old_suggestions = """    public async Task<List<SearchSuggestionViewModel>> GetSearchSuggestionsAsync(string keyword)
    {
        var escapedKeyword = keyword
            .Replace("[", "[[]")
            .Replace("%", "[%]")
            .Replace("_", "[_]");

        var telexKeyword = ClothingStore.Helpers.VietnameseStringHelper.NormalizeTelexForSearch(escapedKeyword);

        var query = dbContext.Products.AsNoTracking().Where(x => x.IsActive);
        
        query = query.Where(x =>
            (x.ProductName.Contains(escapedKeyword) ||
             x.ProductName.Contains(telexKeyword) ||
            x.ProductVariants.Any(v => v.SKU.Contains(escapedKeyword))) &&
            x.ProductVariants.Any(v => v.StockQuantity > 0 && v.IsActive)
        );

        query = query.OrderByDescending(x =>
            x.ProductName.StartsWith(escapedKeyword) || x.ProductName.StartsWith(telexKeyword) ? 3 :
            x.ProductName.Contains(escapedKeyword) || x.ProductName.Contains(telexKeyword) ? 2 :
            x.ProductVariants.Any(v => v.SKU.Contains(escapedKeyword)) ? 1 : 0
        ).ThenByDescending(x => x.CreatedAt);

        var today = DateTime.Today;"""

new_suggestions = """    public async Task<List<SearchSuggestionViewModel>> GetSearchSuggestionsAsync(string keyword)
    {
        var kw = keyword?.Trim() ?? string.Empty;
        var normalizedKw = ClothingStore.Helpers.VietnameseStringHelper.NormalizeVietnamese(kw).ToLower();
        var tokens = normalizedKw.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(5).ToList();
        
        var t0 = tokens.Count > 0 ? tokens[0] : "";
        var t1 = tokens.Count > 1 ? tokens[1] : "";
        var t2 = tokens.Count > 2 ? tokens[2] : "";
        var t3 = tokens.Count > 3 ? tokens[3] : "";
        var t4 = tokens.Count > 4 ? tokens[4] : "";

        var hasT0 = tokens.Count > 0;
        var hasT1 = tokens.Count > 1;
        var hasT2 = tokens.Count > 2;
        var hasT3 = tokens.Count > 3;
        var hasT4 = tokens.Count > 4;

        var query = dbContext.Products.AsNoTracking().Where(x => x.IsActive && x.ProductVariants.Any(v => v.StockQuantity > 0 && v.IsActive));

        if (hasT0)
        {
            query = query.Where(x => 
                (hasT0 && (
                    (x.SearchNormalizedName != null && x.SearchNormalizedName.Contains(t0)) ||
                    x.ProductVariants.Any(v => v.SKU.Contains(t0)) ||
                    x.Category.CategoryName.Contains(t0) ||
                    (x.Description != null && x.Description.Contains(t0))
                )) ||
                (hasT1 && (
                    (x.SearchNormalizedName != null && x.SearchNormalizedName.Contains(t1)) ||
                    x.ProductVariants.Any(v => v.SKU.Contains(t1)) ||
                    x.Category.CategoryName.Contains(t1) ||
                    (x.Description != null && x.Description.Contains(t1))
                )) ||
                (hasT2 && (
                    (x.SearchNormalizedName != null && x.SearchNormalizedName.Contains(t2)) ||
                    x.ProductVariants.Any(v => v.SKU.Contains(t2)) ||
                    x.Category.CategoryName.Contains(t2) ||
                    (x.Description != null && x.Description.Contains(t2))
                )) ||
                (hasT3 && (
                    (x.SearchNormalizedName != null && x.SearchNormalizedName.Contains(t3)) ||
                    x.ProductVariants.Any(v => v.SKU.Contains(t3)) ||
                    x.Category.CategoryName.Contains(t3) ||
                    (x.Description != null && x.Description.Contains(t3))
                )) ||
                (hasT4 && (
                    (x.SearchNormalizedName != null && x.SearchNormalizedName.Contains(t4)) ||
                    x.ProductVariants.Any(v => v.SKU.Contains(t4)) ||
                    x.Category.CategoryName.Contains(t4) ||
                    (x.Description != null && x.Description.Contains(t4))
                ))
            );

            query = query.OrderByDescending(x =>
                (hasT0 ? (
                    (x.SearchNormalizedName != null && x.SearchNormalizedName.StartsWith(t0) ? 100 : 
                     x.SearchNormalizedName != null && x.SearchNormalizedName.Contains(t0) ? 80 : 0) +
                    (x.ProductVariants.Any(v => v.SKU.StartsWith(t0)) ? 60 :
                     x.ProductVariants.Any(v => v.SKU.Contains(t0)) ? 50 : 0) +
                    (x.Category.CategoryName.Contains(t0) ? 30 : 0) +
                    (x.Description != null && x.Description.Contains(t0) ? 10 : 0)
                ) : 0) +
                (hasT1 ? (
                    (x.SearchNormalizedName != null && x.SearchNormalizedName.StartsWith(t1) ? 100 : 
                     x.SearchNormalizedName != null && x.SearchNormalizedName.Contains(t1) ? 80 : 0) +
                    (x.ProductVariants.Any(v => v.SKU.StartsWith(t1)) ? 60 :
                     x.ProductVariants.Any(v => v.SKU.Contains(t1)) ? 50 : 0) +
                    (x.Category.CategoryName.Contains(t1) ? 30 : 0) +
                    (x.Description != null && x.Description.Contains(t1) ? 10 : 0)
                ) : 0) +
                (hasT2 ? (
                    (x.SearchNormalizedName != null && x.SearchNormalizedName.StartsWith(t2) ? 100 : 
                     x.SearchNormalizedName != null && x.SearchNormalizedName.Contains(t2) ? 80 : 0) +
                    (x.ProductVariants.Any(v => v.SKU.StartsWith(t2)) ? 60 :
                     x.ProductVariants.Any(v => v.SKU.Contains(t2)) ? 50 : 0) +
                    (x.Category.CategoryName.Contains(t2) ? 30 : 0) +
                    (x.Description != null && x.Description.Contains(t2) ? 10 : 0)
                ) : 0) +
                (hasT3 ? (
                    (x.SearchNormalizedName != null && x.SearchNormalizedName.StartsWith(t3) ? 100 : 
                     x.SearchNormalizedName != null && x.SearchNormalizedName.Contains(t3) ? 80 : 0) +
                    (x.ProductVariants.Any(v => v.SKU.StartsWith(t3)) ? 60 :
                     x.ProductVariants.Any(v => v.SKU.Contains(t3)) ? 50 : 0) +
                    (x.Category.CategoryName.Contains(t3) ? 30 : 0) +
                    (x.Description != null && x.Description.Contains(t3) ? 10 : 0)
                ) : 0) +
                (hasT4 ? (
                    (x.SearchNormalizedName != null && x.SearchNormalizedName.StartsWith(t4) ? 100 : 
                     x.SearchNormalizedName != null && x.SearchNormalizedName.Contains(t4) ? 80 : 0) +
                    (x.ProductVariants.Any(v => v.SKU.StartsWith(t4)) ? 60 :
                     x.ProductVariants.Any(v => v.SKU.Contains(t4)) ? 50 : 0) +
                    (x.Category.CategoryName.Contains(t4) ? 30 : 0) +
                    (x.Description != null && x.Description.Contains(t4) ? 10 : 0)
                ) : 0)
            ).ThenByDescending(x => x.CreatedAt);
        }
        else
        {
            query = query.OrderByDescending(x => x.CreatedAt);
        }

        var today = DateTime.Today;"""

# Replace SearchProductsAsync
old_search = """        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            // Removed .ToLower() to preserve SQL Server indexing (case-insensitive by default)
            var kw = filter.Keyword;
            var telexKw = ClothingStore.Helpers.VietnameseStringHelper.NormalizeTelexForSearch(kw);
            
            query = query.Where(x => 
                x.ProductName.Contains(kw) || 
                x.ProductName.Contains(telexKw) || 
                x.ProductVariants.Any(v => v.SKU.Contains(kw)) || 
                x.Category.CategoryName.Contains(kw) || 
                x.Category.CategoryName.Contains(telexKw) || 
                (x.Description != null && (x.Description.Contains(kw) || x.Description.Contains(telexKw))) // [Backlog] Future Improvement: SQL Server Full-Text Search
            );

            var isRelevanceSort = string.IsNullOrWhiteSpace(filter.Sort) || filter.Sort.ToLower() == "relevance";
            if (isRelevanceSort)
            {
                query = query.OrderByDescending(x =>
                    x.ProductName.StartsWith(kw) || x.ProductName.StartsWith(telexKw) ? 5 :
                    x.ProductName.Contains(kw) || x.ProductName.Contains(telexKw) ? 4 :
                    x.ProductVariants.Any(v => v.SKU.Contains(kw)) ? 3 :
                    x.Category.CategoryName.Contains(kw) || x.Category.CategoryName.Contains(telexKw) ? 2 :
                    (x.Description != null && (x.Description.Contains(kw) || x.Description.Contains(telexKw))) ? 1 : 0
                ).ThenByDescending(x => x.CreatedAt);
            }
        }"""

new_search = """        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var kw = filter.Keyword?.Trim() ?? string.Empty;
            var normalizedKw = ClothingStore.Helpers.VietnameseStringHelper.NormalizeVietnamese(kw).ToLower();
            var tokens = normalizedKw.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(5).ToList();
            
            var t0 = tokens.Count > 0 ? tokens[0] : "";
            var t1 = tokens.Count > 1 ? tokens[1] : "";
            var t2 = tokens.Count > 2 ? tokens[2] : "";
            var t3 = tokens.Count > 3 ? tokens[3] : "";
            var t4 = tokens.Count > 4 ? tokens[4] : "";

            var hasT0 = tokens.Count > 0;
            var hasT1 = tokens.Count > 1;
            var hasT2 = tokens.Count > 2;
            var hasT3 = tokens.Count > 3;
            var hasT4 = tokens.Count > 4;
            
            query = query.Where(x => 
                (hasT0 && (
                    (x.SearchNormalizedName != null && x.SearchNormalizedName.Contains(t0)) ||
                    x.ProductVariants.Any(v => v.SKU.Contains(t0)) ||
                    x.Category.CategoryName.Contains(t0) ||
                    (x.Description != null && x.Description.Contains(t0))
                )) ||
                (hasT1 && (
                    (x.SearchNormalizedName != null && x.SearchNormalizedName.Contains(t1)) ||
                    x.ProductVariants.Any(v => v.SKU.Contains(t1)) ||
                    x.Category.CategoryName.Contains(t1) ||
                    (x.Description != null && x.Description.Contains(t1))
                )) ||
                (hasT2 && (
                    (x.SearchNormalizedName != null && x.SearchNormalizedName.Contains(t2)) ||
                    x.ProductVariants.Any(v => v.SKU.Contains(t2)) ||
                    x.Category.CategoryName.Contains(t2) ||
                    (x.Description != null && x.Description.Contains(t2))
                )) ||
                (hasT3 && (
                    (x.SearchNormalizedName != null && x.SearchNormalizedName.Contains(t3)) ||
                    x.ProductVariants.Any(v => v.SKU.Contains(t3)) ||
                    x.Category.CategoryName.Contains(t3) ||
                    (x.Description != null && x.Description.Contains(t3))
                )) ||
                (hasT4 && (
                    (x.SearchNormalizedName != null && x.SearchNormalizedName.Contains(t4)) ||
                    x.ProductVariants.Any(v => v.SKU.Contains(t4)) ||
                    x.Category.CategoryName.Contains(t4) ||
                    (x.Description != null && x.Description.Contains(t4))
                ))
            );

            var isRelevanceSort = string.IsNullOrWhiteSpace(filter.Sort) || filter.Sort.ToLower() == "relevance";
            if (isRelevanceSort)
            {
                query = query.OrderByDescending(x =>
                    (hasT0 ? (
                        (x.SearchNormalizedName != null && x.SearchNormalizedName.StartsWith(t0) ? 100 : 
                         x.SearchNormalizedName != null && x.SearchNormalizedName.Contains(t0) ? 80 : 0) +
                        (x.ProductVariants.Any(v => v.SKU.StartsWith(t0)) ? 60 :
                         x.ProductVariants.Any(v => v.SKU.Contains(t0)) ? 50 : 0) +
                        (x.Category.CategoryName.Contains(t0) ? 30 : 0) +
                        (x.Description != null && x.Description.Contains(t0) ? 10 : 0)
                    ) : 0) +
                    (hasT1 ? (
                        (x.SearchNormalizedName != null && x.SearchNormalizedName.StartsWith(t1) ? 100 : 
                         x.SearchNormalizedName != null && x.SearchNormalizedName.Contains(t1) ? 80 : 0) +
                        (x.ProductVariants.Any(v => v.SKU.StartsWith(t1)) ? 60 :
                         x.ProductVariants.Any(v => v.SKU.Contains(t1)) ? 50 : 0) +
                        (x.Category.CategoryName.Contains(t1) ? 30 : 0) +
                        (x.Description != null && x.Description.Contains(t1) ? 10 : 0)
                    ) : 0) +
                    (hasT2 ? (
                        (x.SearchNormalizedName != null && x.SearchNormalizedName.StartsWith(t2) ? 100 : 
                         x.SearchNormalizedName != null && x.SearchNormalizedName.Contains(t2) ? 80 : 0) +
                        (x.ProductVariants.Any(v => v.SKU.StartsWith(t2)) ? 60 :
                         x.ProductVariants.Any(v => v.SKU.Contains(t2)) ? 50 : 0) +
                        (x.Category.CategoryName.Contains(t2) ? 30 : 0) +
                        (x.Description != null && x.Description.Contains(t2) ? 10 : 0)
                    ) : 0) +
                    (hasT3 ? (
                        (x.SearchNormalizedName != null && x.SearchNormalizedName.StartsWith(t3) ? 100 : 
                         x.SearchNormalizedName != null && x.SearchNormalizedName.Contains(t3) ? 80 : 0) +
                        (x.ProductVariants.Any(v => v.SKU.StartsWith(t3)) ? 60 :
                         x.ProductVariants.Any(v => v.SKU.Contains(t3)) ? 50 : 0) +
                        (x.Category.CategoryName.Contains(t3) ? 30 : 0) +
                        (x.Description != null && x.Description.Contains(t3) ? 10 : 0)
                    ) : 0) +
                    (hasT4 ? (
                        (x.SearchNormalizedName != null && x.SearchNormalizedName.StartsWith(t4) ? 100 : 
                         x.SearchNormalizedName != null && x.SearchNormalizedName.Contains(t4) ? 80 : 0) +
                        (x.ProductVariants.Any(v => v.SKU.StartsWith(t4)) ? 60 :
                         x.ProductVariants.Any(v => v.SKU.Contains(t4)) ? 50 : 0) +
                        (x.Category.CategoryName.Contains(t4) ? 30 : 0) +
                        (x.Description != null && x.Description.Contains(t4) ? 10 : 0)
                    ) : 0)
                ).ThenByDescending(x => x.CreatedAt);
            }
        }"""

if old_suggestions in content:
    content = content.replace(old_suggestions, new_suggestions)
else:
    print("Could not find old_suggestions block")

if old_search in content:
    content = content.replace(old_search, new_search)
else:
    print("Could not find old_search block")

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)
print("Patch applied.")
