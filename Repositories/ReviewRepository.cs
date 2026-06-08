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
                AverageRating = g.Average(r => (double)r.Rating),
                Star5Count = g.Count(r => r.Rating == 5),
                Star4Count = g.Count(r => r.Rating == 4),
                Star3Count = g.Count(r => r.Rating == 3),
                Star2Count = g.Count(r => r.Rating == 2),
                Star1Count = g.Count(r => r.Rating == 1)
            })
            .FirstOrDefaultAsync();

        return stats ?? new ReviewStatsDto { TotalReviews = 0, AverageRating = 0 };
    }

    public async Task<(List<ClothingStore.Models.ViewModels.ReviewViewModel> Reviews, int TotalCount)> GetApprovedReviewsAsync(ClothingStore.Models.ViewModels.ReviewFilterParams filter)
    {
        var query = dbContext.Reviews
            .AsNoTracking()
            .Where(r => r.ProductID == filter.ProductId && r.IsApproved);

        if (filter.Rating.HasValue)
        {
            query = query.Where(r => r.Rating == filter.Rating.Value);
        }

        if (!string.IsNullOrEmpty(filter.Sentiment))
        {
            var s = filter.Sentiment.ToLowerInvariant();
            if (s == "positive") query = query.Where(r => r.Rating >= 4);
            else if (s == "neutral") query = query.Where(r => r.Rating == 3);
            else if (s == "negative") query = query.Where(r => r.Rating <= 2);
        }

        var totalCount = await query.CountAsync();

        var projectedQuery = query.Select(r => new ClothingStore.Models.ViewModels.ReviewViewModel
        {
            ReviewID = r.ReviewID,
            Rating = r.Rating,
            Comment = r.Comment,
            ReviewDate = r.ReviewDate,
            CustomerName = r.Customer.FullName,
            Images = r.ReviewImages.Select(img => img.ImageURL).ToList(),
            HelpfulCount = r.ReviewHelpfulVotes.Count(),
            IsCurrentUserVoted = filter.CurrentCustomerId.HasValue && r.ReviewHelpfulVotes.Any(v => v.CustomerId == filter.CurrentCustomerId.Value)
        });

        var sort = (filter.Sort ?? "newest").ToLowerInvariant();
        projectedQuery = sort switch
        {
            "oldest" => projectedQuery.OrderBy(r => r.ReviewDate),
            "highest" => projectedQuery.OrderByDescending(r => r.Rating).ThenByDescending(r => r.ReviewDate),
            "lowest" => projectedQuery.OrderBy(r => r.Rating).ThenByDescending(r => r.ReviewDate),
            "mosthelpful" => projectedQuery.OrderByDescending(r => r.HelpfulCount).ThenByDescending(r => r.ReviewDate),
            _ => projectedQuery.OrderByDescending(r => r.ReviewDate) // newest
        };

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 10 : (filter.PageSize > 50 ? 50 : filter.PageSize);

        var reviews = await projectedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (reviews, totalCount);
    }

    public async Task<(bool IsVoted, int HelpfulCount)> ToggleHelpfulVoteAsync(int reviewId, int customerId)
    {
        // Must check if review exists, is approved, and belongs to a product
        var isValidReview = await dbContext.Reviews.AnyAsync(r => r.ReviewID == reviewId && r.IsApproved);
        if (!isValidReview) return (false, 0);

        var existingVote = await dbContext.ReviewHelpfulVotes
            .FirstOrDefaultAsync(v => v.ReviewID == reviewId && v.CustomerId == customerId);

        if (existingVote != null)
        {
            dbContext.ReviewHelpfulVotes.Remove(existingVote);
            try
            {
                await dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Already removed by another thread
            }
        }
        else
        {
            dbContext.ReviewHelpfulVotes.Add(new ReviewHelpfulVote
            {
                ReviewID = reviewId,
                CustomerId = customerId,
                VotedAt = DateTime.UtcNow
            });
            try
            {
                await dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Ignore spam clicks (already added by another thread)
            }
        }

        // Query the true final state
        var isVoted = await dbContext.ReviewHelpfulVotes.AnyAsync(v => v.ReviewID == reviewId && v.CustomerId == customerId);
        var helpfulCount = await dbContext.ReviewHelpfulVotes.CountAsync(v => v.ReviewID == reviewId);
        return (isVoted, helpfulCount);
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

    public Task<List<Review>> GetReviewsByCustomerAsync(int customerId)
    {
        return dbContext.Reviews
            .AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.ReviewImages)
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.ReviewDate)
            .ToListAsync();
    }

    public Task<Review?> GetReviewByIdAsync(int reviewId)
    {
        return dbContext.Reviews
            .Include(x => x.ReviewImages)
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.ReviewID == reviewId);
    }

    public async Task UpdateReviewAsync(Review review)
    {
        dbContext.Reviews.Update(review);
        await dbContext.SaveChangesAsync();
    }
}
