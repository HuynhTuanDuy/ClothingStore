using System.ComponentModel.DataAnnotations;

namespace ClothingStore.Models.ViewModels;

public class DiscountProgramFilter
{
    public string? SearchKeyword { get; set; }
    public string? Status { get; set; } // "Active", "Inactive", "Upcoming", "Expired"
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class DiscountProgramDashboardStats
{
    public int TotalPrograms { get; set; }
    public int ActivePrograms { get; set; }
    public int UpcomingPrograms { get; set; }
    public int ExpiredPrograms { get; set; }
}

public class DiscountProgramEditViewModel
{
    public int ProgramID { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên chương trình.")]
    public string ProgramName { get; set; } = string.Empty;

    [Range(0, 100, ErrorMessage = "Phần trăm giảm giá phải từ 0 đến 100.")]
    public int DiscountPercent { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu.")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc.")]
    public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(1);

    public bool IsActive { get; set; } = true;

    public byte[]? RowVersion { get; set; }
}
