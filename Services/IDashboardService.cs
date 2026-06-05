using ClothingStore.Models.ViewModels;

namespace ClothingStore.Services;

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardAsync(int? year = null);
}
