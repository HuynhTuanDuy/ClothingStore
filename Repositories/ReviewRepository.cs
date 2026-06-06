using ClothingStore.Data;
using ClothingStore.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClothingStore.Repositories;

public class ReviewRepository(StoreDbContext dbContext) : IReviewRepository
{
    public async Task<ReviewStatsDto> GetReviewStatsAsync(int productId)
    {
        var stats = await dbContext.Reviews
            .AsNoTracking()
            .Where(r => r.ProductID == productId && r.IsApproved)
            .GroupBy(r => r.ProductID)
            .Select(g => new ReviewStatsDto
            {
                TotalReviews = g.Count(),
                AverageRating = g.Average(r => (double)r.Rating)
            })
            .FirstOrDefaultAsync();

        return stats ?? new ReviewStatsDto { TotalReviews = 0, AverageRating = 0 };
    }

    public Task<List<Review>> GetApprovedReviewsAsync(int productId, int page, int pageSize)
    {
        return dbContext.Reviews
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.ReviewImages)
            .Where(r => r.ProductID == productId && r.IsApproved)
            .OrderByDescending(r => r.ReviewDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<bool> CanUserReviewProductAsync(int customerId, int productId)
    {
        var orderId = await GetLatestEligibleOrderIdAsync(customerId, productId);
        return orderId.HasValue;
    }

    public async Task<int?> GetLatestEligibleOrderIdAsync(int customerId, int productId)
    {
        var validOrder = await dbContext.OrderDetails
            .AsNoTracking()
            .Where(od => od.Order.CustomerId == customerId
                      && od.ProductVariant.ProductID == productId
                      && od.Order.OrderStatus == OrderStatus.Delivered
                      && !dbContext.Reviews.Any(r => r.OrderID == od.OrderID && r.ProductID == productId))
            .OrderByDescending(od => od.Order.OrderDate)
            .Select(od => od.OrderID)
            .FirstOrDefaultAsync();

        return validOrder > 0 ? validOrder : null;
    }

    public async Task CreateReviewAsync(Review review)
    {
        await dbContext.Reviews.AddAsync(review);
        await dbContext.SaveChangesAsync();
    }

    public async Task ApproveReviewAsync(int reviewId)
    {
        var review = await dbContext.Reviews.FindAsync(reviewId);
        if (review != null)
        {
            review.IsApproved = true;
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task DeleteReviewAsync(int reviewId)
    {
        var review = await dbContext.Reviews.FindAsync(reviewId);
        if (review != null)
        {
            dbContext.Reviews.Remove(review);
            await dbContext.SaveChangesAsync();
        }
    }

    public Task<List<Review>> GetPendingReviewsAsync()
    {
        return dbContext.Reviews
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Product)
            .Where(r => !r.IsApproved)
            .OrderBy(r => r.ReviewDate)
            .ToListAsync();
    }

    public Task<List<Review>> GetAllReviewsForAdminAsync(int page = 1, int pageSize = 20)
    {
        return dbContext.Reviews
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Product)
            .OrderByDescending(r => r.ReviewDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public Task<int> CountAllReviewsForAdminAsync()
    {
        return dbContext.Reviews.CountAsync();
    }

    public Task<Review?> GetReviewByIdAsync(int reviewId)
    {
        return dbContext.Reviews
            .Include(x => x.ReviewImages)
            .FirstOrDefaultAsync(x => x.ReviewID == reviewId);
    }
}
