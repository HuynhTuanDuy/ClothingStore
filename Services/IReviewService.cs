using ClothingStore.Models.ViewModels;

namespace ClothingStore.Services;

public interface IReviewService
{
    Task<ReviewStatsViewModel> GetReviewStatsAsync(int productId);
    Task<List<ReviewViewModel>> GetProductReviewsAsync(int productId, int page = 1, int pageSize = 10);
    Task<bool> CanUserReviewProductAsync(int customerId, int productId);
    
    // Auto Order Detection + Create Review
    Task<(bool Success, string Message)> CreateReviewAsync(int customerId, CreateReviewViewModel model);
    
    // Admin functions
    Task<AdminReviewPageViewModel> GetAdminReviewsAsync(int page = 1, int pageSize = 20);
    Task<bool> ApproveReviewAsync(int reviewId);
    Task<bool> DeleteReviewAsync(int reviewId);
}
