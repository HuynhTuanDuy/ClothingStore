namespace ClothingStore.Models.ViewModels;

public class ProductSearchFilter
{
    public string? Keyword { get; set; }
    public string Sort { get; set; } = "relevance";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}
