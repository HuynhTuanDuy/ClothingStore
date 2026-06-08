using ClothingStore.Models.Entities;
using ClothingStore.Models.ViewModels;

namespace ClothingStore.Repositories;

public class ReviewStatsDto
{
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int Star5Count { get; set; }
    public int Star4Count { get; set; }
    public int Star3Count { get; set; }
    public int Star2Count { get; set; }
    public int Star1Count { get; set; }
}

public interface IReviewRepository
{
    Task<ReviewStatsDto> GetReviewStatsAsync(int productId);
    Task<(List<ReviewViewModel> Reviews, int TotalCount)> GetApprovedReviewsAsync(ReviewFilterParams filter);
    Task<(bool IsVoted, int HelpfulCount)> ToggleHelpfulVoteAsync(int reviewId, int customerId);
    Task<bool> CanUserReviewProductAsync(int customerId, int productId);
    Task<int?> GetLatestEligibleOrderIdAsync(int customerId, int productId);
    Task CreateReviewAsync(Review review);
    Task ApproveReviewAsync(int reviewId);
    Task DeleteReviewAsync(int reviewId);
    Task<List<Review>> GetPendingReviewsAsync();
    Task<List<Review>> GetAllReviewsForAdminAsync(int page = 1, int pageSize = 20);
    Task<int> CountAllReviewsForAdminAsync();
    Task<List<Review>> GetReviewsByCustomerAsync(int customerId);
    Task<Review?> GetReviewByIdAsync(int reviewId);
    Task UpdateReviewAsync(Review review);
}
