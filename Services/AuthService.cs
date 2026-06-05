using System.Security.Claims;
using ClothingStore.Data;
using ClothingStore.Models.Entities;
using ClothingStore.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace ClothingStore.Services;

public interface IAuthService
{
    Task<(bool Success, string? Error)> LoginAsync(string usernameOrEmail, string password, bool rememberMe, HttpContext httpContext);
    Task<(bool Success, string? Error)> RegisterAsync(RegisterInputModel input);
    Task LogoutAsync(HttpContext httpContext);
    Task<Account?> GetCurrentAccountAsync();
}

public record RegisterInputModel(
    string UserName,
    string Email,
    string Password,
    string FullName,
    string Phone
);

public class AuthService(
    IAccountRepository accountRepository,
    ICartRepository cartRepository,
    IUnitOfWork unitOfWork,
    StoreDbContext dbContext,
    IHttpContextAccessor httpContextAccessor) : IAuthService
{
    public async Task<(bool Success, string? Error)> LoginAsync(
        string usernameOrEmail, string password, bool rememberMe, HttpContext httpContext)
    {
        // Find account by username or email
        var account = await accountRepository.GetByUsernameAsync(usernameOrEmail)
                   ?? await accountRepository.GetByEmailAsync(usernameOrEmail);

        if (account is null)
            return (false, "Tên đăng nhập hoặc mật khẩu không đúng.");

        if (account.Status != AccountStatus.Active)
            return (false, "Tài khoản của bạn đã bị vô hiệu hóa.");

        if (!BCrypt.Net.BCrypt.Verify(password, account.PasswordHash))
            return (false, "Tên đăng nhập hoặc mật khẩu không đúng.");

        // Update LastLoginAt
        var tracked = await dbContext.Accounts.FindAsync(account.UserId);
        if (tracked is not null)
        {
            tracked.LastLoginAt = DateTime.UtcNow;
            await unitOfWork.SaveChangesAsync();
        }

        // Merge guest cart → user cart (HIGH-07)
        if (account.CustomerId.HasValue)
        {
            var sessionKey = httpContext.Session.GetString(CartService.SessionCartKey);
            if (!string.IsNullOrWhiteSpace(sessionKey))
            {
                var guestCart = await cartRepository.GetActiveCartBySessionKeyAsync(sessionKey);
                if (guestCart?.CartItems.Count > 0)
                {
                    var userCart = await cartRepository.GetActiveCartByCustomerIdAsync(account.CustomerId.Value);
                    if (userCart is null)
                    {
                        // Assign guest cart to user
                        guestCart.CustomerId = account.CustomerId.Value;
                        guestCart.SessionKey = null;
                        guestCart.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        await cartRepository.MergeGuestCartAsync(guestCart, userCart);
                    }
                    await unitOfWork.SaveChangesAsync();
                }
                httpContext.Session.Remove(CartService.SessionCartKey);
            }
        }

        // Build claims principal
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.UserId.ToString()),
            new(ClaimTypes.Name, account.UserName),
            new(ClaimTypes.Email, account.Email),
        };

        if (account.CustomerId.HasValue)
            claims.Add(new Claim("CustomerId", account.CustomerId.Value.ToString()));

        if (account.Customer?.FullName is not null)
            claims.Add(new Claim("FullName", account.Customer.FullName));

        foreach (var role in account.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = rememberMe, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) });

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RegisterAsync(RegisterInputModel input)
    {
        if (await accountRepository.UsernameExistsAsync(input.UserName))
            return (false, "Tên đăng nhập đã được sử dụng.");

        if (await accountRepository.EmailExistsAsync(input.Email))
            return (false, "Email đã được đăng ký.");

        if (await dbContext.Customers.AnyAsync(x => x.Phone == input.Phone))
            return (false, "Số điện thoại đã được đăng ký.");

        // Get default (lowest) membership
        var defaultMembership = await dbContext.Memberships
            .OrderBy(x => x.MinPoint)
            .FirstOrDefaultAsync();

        if (defaultMembership is null)
            return (false, "Hệ thống chưa cấu hình hạng thành viên. Vui lòng liên hệ admin.");

        // Get Customer role
        var customerRole = await dbContext.Roles
            .FirstOrDefaultAsync(x => x.NormalizedName == "CUSTOMER");

        if (customerRole is null)
            return (false, "Hệ thống chưa cấu hình phân quyền. Vui lòng liên hệ admin.");

        var now = DateTime.UtcNow;

        var customer = new Customer
        {
            FullName     = input.FullName.Trim(),
            Phone        = input.Phone.Trim(),
            Email        = input.Email.Trim().ToLowerInvariant(),
            MembershipID = defaultMembership.MembershipID,
            CreatedAt    = now,
            UpdatedAt    = now
        };

        await dbContext.Customers.AddAsync(customer);
        await unitOfWork.SaveChangesAsync(); // get CustomerId

        var account = new Account
        {
            UserName     = input.UserName.Trim(),
            Email        = input.Email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.Password),
            Status       = AccountStatus.Active,
            CustomerId   = customer.CustomerId,
            CreatedAt    = now,
            UpdatedAt    = now
        };

        await accountRepository.AddAccountAsync(account);
        await unitOfWork.SaveChangesAsync(); // get UserId

        account.AccountRoles.Add(new AccountRole
        {
            UserId = account.UserId,
            RoleId = customerRole.RoleId
        });

        await unitOfWork.SaveChangesAsync();
        return (true, null);
    }

    public async Task LogoutAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        httpContext.Session.Clear();
    }

    public async Task<Account?> GetCurrentAccountAsync()
    {
        var ctx = httpContextAccessor.HttpContext;
        var idStr = ctx?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idStr, out var userId)) return null;
        return await accountRepository.GetByIdAsync(userId);
    }
}
