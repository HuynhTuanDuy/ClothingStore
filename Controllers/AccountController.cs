using ClothingStore.Models.ViewModels;
using ClothingStore.Services;
using Microsoft.AspNetCore.Mvc;

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
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await authService.RegisterAsync(new RegisterInputModel(
            model.UserName, model.Email, model.Password, model.FullName, model.Phone));

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Đăng ký thất bại.");
            return View(model);
        }

        TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
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
}
