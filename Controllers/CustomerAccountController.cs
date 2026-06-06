using ClothingStore.Models.ViewModels;
using ClothingStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClothingStore.Controllers;

[Authorize]
[Route("Account")]
public class CustomerAccountController(
    ICustomerAccountService customerAccountService,
    ICurrentCustomerService currentCustomerService,
    IAuthService authService,
    ILogger<CustomerAccountController> logger) : Controller
{
    private int GetCustomerId() => currentCustomerService.GetCustomerId() ?? 0;
    private int GetAccountId() => currentCustomerService.GetUserId() ?? 0;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return RedirectToAction("Login", "Account");

        var model = await customerAccountService.GetDashboardAsync(customerId);
        if (model == null) return RedirectToAction("Login", "Account");

        return View(model);
    }

    [HttpGet("Profile")]
    public async Task<IActionResult> Profile()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return RedirectToAction("Login", "Account");

        var model = await customerAccountService.GetProfileAsync(customerId);
        if (model == null) return NotFound();

        return View(model);
    }

    [HttpPost("Profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(CustomerProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var customerId = GetCustomerId();
        var (success, error) = await customerAccountService.UpdateProfileAsync(customerId, model);

        if (success)
        {
            TempData["Success"] = "Cập nhật thông tin thành công.";
            return RedirectToAction(nameof(Profile));
        }

        ModelState.AddModelError(string.Empty, error ?? "Cập nhật thất bại.");
        return View(model);
    }

    [HttpGet("ChangePassword")]
    public IActionResult ChangePassword()
    {
        return View(new CustomerChangePasswordViewModel());
    }

    [HttpPost("ChangePassword")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(CustomerChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var accountId = GetAccountId();
        var (success, error) = await customerAccountService.ChangePasswordAsync(accountId, model);

        if (success)
        {
            await authService.LogoutAsync(HttpContext);
            TempData["Success"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.";
            return RedirectToAction("Login", "Account");
        }

        ModelState.AddModelError(string.Empty, error ?? "Đổi mật khẩu thất bại.");
        return View(model);
    }

    [HttpGet("Orders")]
    public async Task<IActionResult> Orders(int page = 1)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return RedirectToAction("Login", "Account");

        var model = await customerAccountService.GetCustomerOrdersAsync(customerId, page);
        return View(model);
    }

    [HttpGet("OrderDetail/{orderCode}")]
    public async Task<IActionResult> OrderDetail(string orderCode)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return RedirectToAction("Login", "Account");

        var order = await customerAccountService.GetCustomerOrderDetailsAsync(customerId, orderCode);
        
        // Ownership Validation
        if (order == null || order.CustomerId != customerId)
        {
            logger.LogWarning("Customer {CustomerId} attempted to view order {OrderCode} which does not belong to them.", customerId, orderCode);
            return Forbid();
        }

        return View(order);
    }

    [HttpGet("Addresses")]
    public async Task<IActionResult> Addresses()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return RedirectToAction("Login", "Account");

        var addresses = await customerAccountService.GetAddressesAsync(customerId);
        return View(addresses);
    }

    [HttpPost("AddAddress")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAddress(AddressFormViewModel model)
    {
        var customerId = GetCustomerId();
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Vui lòng nhập đầy đủ thông tin bắt buộc.";
            return RedirectToAction(nameof(Addresses));
        }

        var (success, error) = await customerAccountService.CreateAddressAsync(customerId, model);
        if (success) TempData["Success"] = "Thêm địa chỉ thành công.";
        else TempData["Error"] = error;

        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost("EditAddress")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAddress(AddressFormViewModel model)
    {
        var customerId = GetCustomerId();
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Vui lòng nhập đầy đủ thông tin bắt buộc.";
            return RedirectToAction(nameof(Addresses));
        }

        var (success, error) = await customerAccountService.UpdateAddressAsync(customerId, model);
        if (success) TempData["Success"] = "Cập nhật địa chỉ thành công.";
        else TempData["Error"] = error;

        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost("DeleteAddress")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAddress(int addressId)
    {
        var customerId = GetCustomerId();
        var (success, error) = await customerAccountService.DeleteAddressAsync(customerId, addressId);
        
        if (success) TempData["Success"] = "Xóa địa chỉ thành công.";
        else TempData["Error"] = error;

        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost("SetDefaultAddress")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefaultAddress(int addressId)
    {
        var customerId = GetCustomerId();
        var (success, error) = await customerAccountService.SetDefaultAddressAsync(customerId, addressId);
        
        if (success) TempData["Success"] = "Đã đặt làm địa chỉ mặc định.";
        else TempData["Error"] = error;

        return RedirectToAction(nameof(Addresses));
    }
}
