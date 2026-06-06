using ClothingStore.Models.Entities;

namespace ClothingStore.Repositories;

public class ReviewStatsDto
{
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
}

public interface IReviewRepository
{
    Task<ReviewStatsDto> GetReviewStatsAsync(int productId);
    Task<List<Review>> GetApprovedReviewsAsync(int productId, int page, int pageSize);
    Task<bool> CanUserReviewProductAsync(int customerId, int productId);
    Task<int?> GetLatestEligibleOrderIdAsync(int customerId, int productId);
    Task CreateReviewAsync(Review review);
    Task ApproveReviewAsync(int reviewId);
    Task DeleteReviewAsync(int reviewId);
    Task<List<Review>> GetPendingReviewsAsync();
    Task<List<Review>> GetAllReviewsForAdminAsync(int page = 1, int pageSize = 20);
    Task<int> CountAllReviewsForAdminAsync();
    Task<Review?> GetReviewByIdAsync(int reviewId);
}
