using ClothingStore.Models.Entities;
using ClothingStore.Models.ViewModels;
using ClothingStore.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

using Microsoft.Extensions.Logging;
using ClothingStore.Data; // if needed, but we use repository

namespace ClothingStore.Services;

public class ReviewService(
    IReviewRepository reviewRepository,
    IWebHostEnvironment environment,
    ILogger<ReviewService> logger,
    ClothingStore.Data.StoreDbContext dbContext) : IReviewService
{
    public async Task<ReviewStatsViewModel> GetReviewStatsAsync(int productId)
    {
        var stats = await reviewRepository.GetReviewStatsAsync(productId);
        return new ReviewStatsViewModel
        {
            AverageRating = stats.AverageRating,
            TotalReviews = stats.TotalReviews,
            Star5Count = stats.Star5Count,
            Star4Count = stats.Star4Count,
            Star3Count = stats.Star3Count,
            Star2Count = stats.Star2Count,
            Star1Count = stats.Star1Count
        };
    }

    public async Task<(List<ReviewViewModel> Reviews, int TotalCount)> GetProductReviewsAsync(ReviewFilterParams filter)
    {
        return await reviewRepository.GetApprovedReviewsAsync(filter);
    }

    public Task<bool> CanUserReviewProductAsync(int customerId, int productId)
    {
        return reviewRepository.CanUserReviewProductAsync(customerId, productId);
    }

    public async Task<(bool Success, string Message)> CreateReviewAsync(int customerId, CreateReviewViewModel model)
    {
        // 1. Auto Order Detection
        var validOrderId = await reviewRepository.GetLatestEligibleOrderIdAsync(customerId, model.ProductID);
        if (validOrderId == null)
        {
            return (false, "Bạn chưa mua sản phẩm này hoặc đã đánh giá rồi.");
        }

        // 2. Validate Images if any
        var allowedExtensions = new HashSet<string> { ".jpg", ".jpeg", ".png", ".webp" };
        var uploadedImageUrls = new List<string>();

        if (model.Images != null && model.Images.Count > 0)
        {
            if (model.Images.Count > 3)
            {
                return (false, "Chỉ được phép upload tối đa 3 ảnh.");
            }

            var uploadRoot = Path.Combine(environment.WebRootPath, "uploads", "reviews");
            if (!Directory.Exists(uploadRoot))
            {
                Directory.CreateDirectory(uploadRoot);
            }

            foreach (var file in model.Images)
            {
                if (file.Length == 0) continue;
                if (file.Length > 2 * 1024 * 1024) // 2MB
                {
                    return (false, "Dung lượng mỗi ảnh không được vượt quá 2MB.");
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    return (false, "Chỉ hỗ trợ định dạng JPG, PNG, WEBP.");
                }

                var fileName = $"review-{Guid.NewGuid():N}{extension}";
                var absolutePath = Path.Combine(uploadRoot, fileName);

                await using (var stream = File.Create(absolutePath))
                {
                    await file.CopyToAsync(stream);
                }

                uploadedImageUrls.Add($"/uploads/reviews/{fileName}");
            }
        }

        // 3. Create Review
        var review = new Review
        {
            ProductID = model.ProductID,
            CustomerId = customerId,
            OrderID = validOrderId.Value,
            Rating = model.Rating,
            Comment = model.Comment,
            ReviewDate = DateTime.UtcNow,
            IsApproved = true, // Auto-approve reviews
            ReviewImages = uploadedImageUrls.Select(url => new ReviewImage { ImageURL = url }).ToList()
        };

        await reviewRepository.CreateReviewAsync(review);
        logger.LogInformation("Review created successfully: Customer {CustomerId} created review for product {ProductId}", customerId, model.ProductID);

        return (true, "Đánh giá của bạn đã được đăng thành công.");
    }

    public async Task<(bool Success, string Message, bool IsVoted, int HelpfulCount)> ToggleHelpfulVoteAsync(int reviewId, int customerId)
    {
        try
        {
            var result = await reviewRepository.ToggleHelpfulVoteAsync(reviewId, customerId);
            if (result.IsVoted)
            {
                logger.LogInformation("ReviewHelpfulToggled: Customer {CustomerId} added helpful vote for review {ReviewId}", customerId, reviewId);
            }
            else
            {
                logger.LogInformation("ReviewHelpfulToggled: Customer {CustomerId} removed helpful vote for review {ReviewId}", customerId, reviewId);
            }
            return (true, result.IsVoted ? "Đã đánh dấu hữu ích." : "Đã bỏ đánh dấu hữu ích.", result.IsVoted, result.HelpfulCount);
        }
        catch (Exception)
        {
            return (false, "Có lỗi xảy ra khi xử lý yêu cầu.", false, 0);
        }
    }

    public async Task<List<CustomerReviewItemViewModel>> GetCustomerReviewsAsync(int customerId)
    {
        var reviews = await reviewRepository.GetReviewsByCustomerAsync(customerId);
        return reviews.Select(r => new CustomerReviewItemViewModel
        {
            ReviewID = r.ReviewID,
            ProductID = r.ProductID,
            ProductName = r.Product?.ProductName ?? "Sản phẩm",
            ProductSlug = r.Product?.Slug ?? "",
            ProductImage = r.Product?.ThumbnailUrl ?? "/images/placeholder.jpg",
            Rating = r.Rating,
            Comment = r.Comment,
            ReviewDate = r.ReviewDate,
            IsApproved = r.IsApproved,
            Images = r.ReviewImages != null ? r.ReviewImages.Select(i => i.ImageURL).ToList() : new List<string>()
        }).ToList();
    }

    public async Task<(bool Success, string Message)> UpdateReviewAsync(int reviewId, int customerId, UpdateReviewViewModel model)
    {
        var review = await reviewRepository.GetReviewByIdAsync(reviewId);
        if (review == null)
        {
            return (false, "Không tìm thấy đánh giá.");
        }

        if (review.CustomerId != customerId)
        {
            logger.LogWarning("Customer {CustomerId} attempted to edit review {ReviewId} not owned by them", customerId, reviewId);
            return (false, "Bạn không có quyền chỉnh sửa đánh giá này.");
        }

        if (review.Product == null || !review.Product.IsActive)
        {
            return (false, "Không thể chỉnh sửa đánh giá cho sản phẩm không còn tồn tại hoặc ngừng kinh doanh.");
        }

        review.Rating = model.Rating;
        review.Comment = model.Comment;
        review.IsApproved = true;

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var oldFilePathsToDelete = new List<string>();

            if (model.Images != null && model.Images.Count > 0)
            {
                if (model.Images.Count > 3)
                {
                    return (false, "Chỉ được phép upload tối đa 3 ảnh.");
                }

                var allowedExtensions = new HashSet<string> { ".jpg", ".jpeg", ".png", ".webp" };
                foreach (var file in model.Images)
                {
                    if (file.Length == 0) continue;
                    if (file.Length > 2 * 1024 * 1024)
                    {
                        return (false, "Dung lượng mỗi ảnh không được vượt quá 2MB.");
                    }
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(extension))
                    {
                        return (false, "Chỉ hỗ trợ định dạng JPG, PNG, WEBP.");
                    }
                }

                var uploadRoot = Path.Combine(environment.WebRootPath, "uploads", "reviews");
                if (!Directory.Exists(uploadRoot))
                {
                    Directory.CreateDirectory(uploadRoot);
                }

                if (review.ReviewImages != null && review.ReviewImages.Any())
                {
                    foreach (var oldImg in review.ReviewImages.ToList())
                    {
                        var oldFileName = Path.GetFileName(oldImg.ImageURL);
                        var oldFilePath = Path.Combine(uploadRoot, oldFileName);
                        oldFilePathsToDelete.Add(oldFilePath);
                        
                        dbContext.Set<ReviewImage>().Remove(oldImg);
                    }
                    review.ReviewImages.Clear();
                }

                if (review.ReviewImages == null)
                {
                    review.ReviewImages = new List<ReviewImage>();
                }

                foreach (var file in model.Images)
                {
                    if (file.Length == 0) continue;
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    var fileName = $"review-{Guid.NewGuid():N}{extension}";
                    var absolutePath = Path.Combine(uploadRoot, fileName);

                    using (var stream = File.Create(absolutePath))
                    {
                        await file.CopyToAsync(stream);
                    }
                    review.ReviewImages.Add(new ReviewImage { ImageURL = $"/uploads/reviews/{fileName}" });
                }
            }

            await reviewRepository.UpdateReviewAsync(review);
            await transaction.CommitAsync();

            logger.LogInformation("Review updated successfully: Customer {CustomerId} updated review {ReviewId}", customerId, reviewId);

            var uploadRootDir = Path.GetFullPath(Path.Combine(environment.WebRootPath, "uploads", "reviews"));
            foreach (var oldPath in oldFilePathsToDelete)
            {
                var fullPath = Path.GetFullPath(oldPath);
                if (fullPath.StartsWith(uploadRootDir) && File.Exists(fullPath))
                {
                    try
                    {
                        File.Delete(fullPath);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Could not delete old review image: {Path}", fullPath);
                    }
                }
            }

            return (true, "Đánh giá của bạn đã được cập nhật thành công.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error updating review {ReviewId}", reviewId);
            return (false, "Có lỗi xảy ra trong quá trình cập nhật đánh giá.");
        }
    }

    public async Task<AdminReviewPageViewModel> GetAdminReviewsAsync(int page = 1, int pageSize = 20)
    {
        var reviews = await reviewRepository.GetAllReviewsForAdminAsync(page, pageSize);
        var total = await reviewRepository.CountAllReviewsForAdminAsync();

        return new AdminReviewPageViewModel
        {
            Reviews = reviews.Select(r => new AdminReviewItemViewModel
            {
                ReviewID = r.ReviewID,
                CustomerName = r.Customer.FullName,
                CustomerEmail = r.Customer.Email ?? string.Empty,
                CustomerPhone = r.Customer.Phone ?? string.Empty,
                ProductName = r.Product.ProductName,
                Rating = r.Rating,
                Comment = r.Comment,
                ReviewDate = r.ReviewDate,
                IsApproved = r.IsApproved,
                Images = r.ReviewImages != null ? r.ReviewImages.Select(i => i.ImageURL).ToList() : []
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<bool> ApproveReviewAsync(int reviewId)
    {
        await reviewRepository.ApproveReviewAsync(reviewId);
        logger.LogInformation("ReviewApproved: Admin approved review {ReviewId}", reviewId);
        return true;
    }

    public async Task<bool> DeleteReviewAsync(int reviewId)
    {
        var review = await reviewRepository.GetReviewByIdAsync(reviewId);
        if (review != null)
        {
            // Delete images from disk
            if (review.ReviewImages != null)
            {
                var uploadRoot = Path.Combine(environment.WebRootPath, "uploads", "reviews");
                foreach (var img in review.ReviewImages)
                {
                    var fileName = Path.GetFileName(img.ImageURL);
                    var filePath = Path.Combine(uploadRoot, fileName);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
            }
            await reviewRepository.DeleteReviewAsync(reviewId);
            return true;
        }
        return false;
    }
}
