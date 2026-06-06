using System.ComponentModel.DataAnnotations;
using ClothingStore.Models.Entities;

namespace ClothingStore.Models.ViewModels;

public class CustomerDashboardViewModel
{
    public string CustomerName { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int ShippingOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int RewardPoints { get; set; }
}

public class CustomerProfileViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [Display(Name = "Họ tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [Display(Name = "Số điện thoại")]
    [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$", ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "Email (Không thể thay đổi)")]
    public string Email { get; set; } = string.Empty;
}

public class CustomerChangePasswordViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu hiện tại")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [StringLength(100, ErrorMessage = "{0} phải có độ dài ít nhất {2} ký tự.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Xác nhận mật khẩu")]
    [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class CustomerOrdersViewModel
{
    public List<Order> Orders { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
}

public class AddressFormViewModel
{
    public int AddressID { get; set; }
    
    [Required(ErrorMessage = "Vui lòng nhập Tên người nhận.")]
    public string RecipientName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Vui lòng nhập Số điện thoại.")]
    [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$", ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string ReceiverPhone { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Vui lòng nhập Tên đường, số nhà.")]
    public string AddressLine { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Vui lòng chọn Phường/Xã.")]
    public string Ward { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Vui lòng chọn Quận/Huyện.")]
    public string District { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Vui lòng chọn Tỉnh/Thành phố.")]
    public string Province { get; set; } = string.Empty;
    
    public bool IsDefault { get; set; }
}
