using ClothingStore.Models.Entities;
using ClothingStore.Models.ViewModels;
using ClothingStore.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace ClothingStore.Services;

public class ReviewService(
    IReviewRepository reviewRepository,
    IWebHostEnvironment environment) : IReviewService
{
    public async Task<ReviewStatsViewModel> GetReviewStatsAsync(int productId)
    {
        var stats = await reviewRepository.GetReviewStatsAsync(productId);
        return new ReviewStatsViewModel
        {
            AverageRating = stats.AverageRating,
            TotalReviews = stats.TotalReviews
        };
    }

    public async Task<List<ReviewViewModel>> GetProductReviewsAsync(int productId, int page = 1, int pageSize = 10)
    {
        var reviews = await reviewRepository.GetApprovedReviewsAsync(productId, page, pageSize);
        return reviews.Select(r => new ReviewViewModel
        {
            ReviewID = r.ReviewID,
            Rating = r.Rating,
            Comment = r.Comment,
            ReviewDate = r.ReviewDate,
            CustomerName = r.Customer.FullName,
            Images = r.ReviewImages.OrderBy(i => i.ImageID).Select(i => i.ImageURL).ToList()
        }).ToList();
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
            IsApproved = false,
            ReviewImages = uploadedImageUrls.Select(url => new ReviewImage { ImageURL = url }).ToList()
        };

        await reviewRepository.CreateReviewAsync(review);

        return (true, "Đánh giá của bạn đã được gửi và đang chờ duyệt.");
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
