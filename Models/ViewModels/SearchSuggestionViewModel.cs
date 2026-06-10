namespace ClothingStore.Models.ViewModels;

public class SearchSuggestionViewModel
{
    public int ProductId { get; set; }
    public string ProductSlug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
}
