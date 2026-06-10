namespace ClothingStore.Models.ViewModels;

public class ProductSearchResultViewModel
{
    public string Keyword { get; set; } = string.Empty;
    public string Sort { get; set; } = "relevance";
    public PagedResult<ProductCardViewModel> Products { get; set; } = new();
}
