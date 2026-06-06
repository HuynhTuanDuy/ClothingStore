using ClothingStore.Models.ViewModels;
using ClothingStore.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClothingStore.Controllers;

public class AccountController(
    IAuthService authService,
    ICurrentCustomerService currentCustomerService) : Controller
{
    // ── GET /Account/Login ──────────────────────────────────────
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (currentCustomerService.IsAuthenticated)
            return RedirectToAction("Index", "Home");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    // ── POST /Account/Login ─────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await authService.LoginAsync(
            model.UsernameOrEmail, model.Password, model.RememberMe, HttpContext);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Đăng nhập thất bại.");
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToAction("Index", "Home");
    }

    // ── GET /Account/Register ───────────────────────────────────
    [HttpGet]
    public IActionResult Register()
    {
        if (currentCustomerService.IsAuthenticated)
            return RedirectToAction("Index", "Home");

        return View(new RegisterViewModel());
    }

    // ── POST /Account/Register ──────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!model.AcceptTerms)
        {
            ModelState.AddModelError(nameof(model.AcceptTerms), "Bạn phải đồng ý với Điều khoản và Dịch vụ.");
        }

        if (!ModelState.IsValid) return View(model);

        var (success, error) = await authService.RegisterAsync(new RegisterInputModel(
            model.UserName, model.Email, model.Password, model.FullName, model.Phone));

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Đăng ký thất bại.");
            return View(model);
        }

        await authService.GenerateEmailVerificationTokenAsync(model.Email);

        TempData["Success"] = "Đăng ký thành công! Vui lòng kiểm tra email để xác minh tài khoản trước khi đăng nhập.";
        return RedirectToAction(nameof(Login));
    }

    // ── GET /Account/VerifyEmail ────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> VerifyEmail(string token)
    {
        var (success, error) = await authService.VerifyEmailAsync(token);
        if (!success)
        {
            TempData["AuthError"] = error;
            return RedirectToAction(nameof(Login));
        }

        TempData["Success"] = "Xác minh email thành công. Bạn có thể đăng nhập ngay.";
        return RedirectToAction(nameof(Login));
    }

    // ── POST /Account/Logout ────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await authService.LogoutAsync(HttpContext);
        return RedirectToAction("Index", "Home");
    }

    // ── GET /Account/AccessDenied ───────────────────────────────
    public IActionResult AccessDenied() => View();

    // ── POST /Account/ExternalLogin ─────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, provider);
    }

    // ── GET /Account/ExternalLoginCallback ──────────────────────
    [HttpGet]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        if (remoteError != null)
        {
            TempData["AuthError"] = $"Lỗi từ nhà cung cấp ngoài: {remoteError}";
            return RedirectToAction(nameof(Login));
        }

        var info = await HttpContext.AuthenticateAsync("ExternalCookie");
        if (!info.Succeeded)
            return RedirectToAction(nameof(Login));

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        var fullName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? email ?? string.Empty;

        if (string.IsNullOrEmpty(email))
        {
            TempData["AuthError"] = "Không thể lấy thông tin Email từ nhà cung cấp.";
            return RedirectToAction(nameof(Login));
        }

        var (success, error) = await authService.ExternalLoginAsync(email, fullName, HttpContext);

        if (!success)
        {
            TempData["AuthError"] = error ?? "Lỗi đăng nhập bằng tài khoản mạng xã hội.";
            return RedirectToAction(nameof(Login));
        }

        // Clean up external cookie
        await HttpContext.SignOutAsync("ExternalCookie");

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    // ── GET /Account/ForgotPassword ─────────────────────────────
    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    // ── POST /Account/ForgotPassword ────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        await authService.GeneratePasswordResetTokenAsync(model.Email);

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    // ── GET /Account/ForgotPasswordConfirmation ─────────────────
    [HttpGet]
    public IActionResult ForgotPasswordConfirmation() => View();

    // ── GET /Account/ResetPassword ──────────────────────────────
    [HttpGet]
    public IActionResult ResetPassword(string token)
    {
        if (string.IsNullOrEmpty(token)) return RedirectToAction("Index", "Home");
        return View(new ResetPasswordViewModel { Token = token });
    }

    // ── POST /Account/ResetPassword ─────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await authService.ResetPasswordAsync(model.Token, model.Password);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Không thể đặt lại mật khẩu.");
            return View(model);
        }

        TempData["Success"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập.";
        return RedirectToAction(nameof(Login));
    }
}
