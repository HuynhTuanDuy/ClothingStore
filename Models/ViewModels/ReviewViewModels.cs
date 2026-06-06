using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ClothingStore.Models.ViewModels;

public class ReviewStatsViewModel
{
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
}

public class CreateReviewViewModel
{
    [Required]
    public int ProductID { get; set; }

    [Required]
    [Range(1, 5, ErrorMessage = "Vui lòng chọn số sao từ 1 đến 5.")]
    public int Rating { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá.")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Nội dung đánh giá từ 10 đến 1000 ký tự.")]
    public string Comment { get; set; } = string.Empty;

    public List<IFormFile>? Images { get; set; }
}

public class AdminReviewPageViewModel
{
    public List<AdminReviewItemViewModel> Reviews { get; set; } = [];
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}

public class AdminReviewItemViewModel
{
    public int ReviewID { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime ReviewDate { get; set; }
    public bool IsApproved { get; set; }
    public List<string> Images { get; set; } = [];
}
