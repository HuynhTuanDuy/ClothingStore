using ClothingStore.Models.ViewModels;

namespace ClothingStore.Services;

public interface IReviewService
{
    Task<ReviewStatsViewModel> GetReviewStatsAsync(int productId);
    Task<(List<ReviewViewModel> Reviews, int TotalCount)> GetProductReviewsAsync(ReviewFilterParams filter);
    Task<bool> CanUserReviewProductAsync(int customerId, int productId);
    
    // Auto Order Detection + Create Review
    Task<(bool Success, string Message)> CreateReviewAsync(int customerId, CreateReviewViewModel model);
    
    Task<(bool Success, string Message, bool IsVoted, int HelpfulCount)> ToggleHelpfulVoteAsync(int reviewId, int customerId);

    // Customer Reviews
    Task<List<CustomerReviewItemViewModel>> GetCustomerReviewsAsync(int customerId);
    Task<(bool Success, string Message)> UpdateReviewAsync(int reviewId, int customerId, UpdateReviewViewModel model);
    
    // Admin functions
    Task<AdminReviewPageViewModel> GetAdminReviewsAsync(int page = 1, int pageSize = 20);
    Task<bool> ApproveReviewAsync(int reviewId);
    Task<bool> DeleteReviewAsync(int reviewId);
}
