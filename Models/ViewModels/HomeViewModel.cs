using System.Collections.Generic;

namespace ClothingStore.Models.ViewModels;

public class HomeViewModel
{
    public List<ProductCardViewModel> BestSellers { get; set; } = [];
    public ProductListViewModel ShopProducts { get; set; } = new();
}
