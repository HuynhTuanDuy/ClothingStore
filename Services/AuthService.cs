using System.Security.Claims;
using ClothingStore.Data;
using ClothingStore.Models.Entities;
using ClothingStore.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ClothingStore.Services;

public interface IAuthService
{
    Task<(bool Success, string? Error)> LoginAsync(string usernameOrEmail, string password, bool rememberMe, HttpContext httpContext);
    Task<(bool Success, string? Error)> RegisterAsync(RegisterInputModel input);
    Task LogoutAsync(HttpContext httpContext);
    Task<Account?> GetCurrentAccountAsync();
    
    // External Auth
    Task<(bool Success, string? Error)> ExternalLoginAsync(string email, string fullName, HttpContext httpContext);
    
    // Forgot Password
    Task<(bool Success, string? Error)> GeneratePasswordResetTokenAsync(string email);
    Task<(bool Success, string? Error)> ResetPasswordAsync(string token, string newPassword);
    
    // Email Verification
    Task<(bool Success, string? Error)> GenerateEmailVerificationTokenAsync(string email);
    Task<(bool Success, string? Error)> VerifyEmailAsync(string token);
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
    IHttpContextAccessor httpContextAccessor,
    Microsoft.Extensions.Caching.Memory.IMemoryCache memoryCache,
    IEmailService emailService) : IAuthService
{
    public async Task<(bool Success, string? Error)> LoginAsync(
        string usernameOrEmail, string password, bool rememberMe, HttpContext httpContext)
    {
        var cacheKey = $"LoginFail_{usernameOrEmail.ToLower()}";
        if (memoryCache.TryGetValue(cacheKey, out int failCount) && failCount >= 5)
        {
            return (false, "Tài khoản tạm bị khóa do đăng nhập sai nhiều lần. Vui lòng thử lại sau 15 phút.");
        }

        void RecordFailedLogin()
        {
            failCount++;
            memoryCache.Set(cacheKey, failCount, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
            });
        }

        // Find account by username or email
        var account = await accountRepository.GetByUsernameAsync(usernameOrEmail)
                   ?? await accountRepository.GetByEmailAsync(usernameOrEmail);

        if (account is null)
        {
            RecordFailedLogin();
            return (false, "Tên đăng nhập hoặc mật khẩu không đúng.");
        }

        if (account.Status != AccountStatus.Active)
            return (false, "Tài khoản của bạn đã bị vô hiệu hóa.");

        if (!BCrypt.Net.BCrypt.Verify(password, account.PasswordHash))
        {
            RecordFailedLogin();
            return (false, "Tên đăng nhập hoặc mật khẩu không đúng.");
        }

        // Optional Toggle: Enforce Email Verification
        bool enforceEmailVerification = false;
        var verifiedKey = $"EmailVerified_{account.UserId}";
        if (enforceEmailVerification)
        {
            if (!memoryCache.TryGetValue(verifiedKey, out bool isVerified) || !isVerified)
            {
                return (false, "Vui lòng kiểm tra email và xác minh tài khoản trước khi đăng nhập.");
            }
        }

        // Reset counter on success
        memoryCache.Remove(cacheKey);

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

        if (memoryCache.TryGetValue(verifiedKey, out bool isClaimVerified) && isClaimVerified)
        {
            claims.Add(new Claim("EmailVerified", "true"));
        }

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

    public async Task<(bool Success, string? Error)> ExternalLoginAsync(string email, string fullName, HttpContext httpContext)
    {
        var account = await accountRepository.GetByEmailAsync(email);

        if (account is null)
        {
            return (false, "Tài khoản email này chưa được đăng ký trong hệ thống. Vui lòng tạo tài khoản trước khi đăng nhập bằng Google/Facebook.");
        }
        if (account.Status != AccountStatus.Active) return (false, "Tài khoản của bạn đã bị vô hiệu hóa.");

        var tracked = await dbContext.Accounts.FindAsync(account.UserId);
        if (tracked is not null)
        {
            tracked.LastLoginAt = DateTime.UtcNow;
            await unitOfWork.SaveChangesAsync();
        }

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

        claims.Add(new Claim("EmailVerified", "true")); // External auth is pre-verified

        var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) });

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> GeneratePasswordResetTokenAsync(string email)
    {
        var account = await accountRepository.GetByEmailAsync(email);
        if (account is null) return (true, null); // Always return true to prevent email enumeration

        var token = Guid.NewGuid().ToString("N");
        var cacheKey = $"ResetToken_{token}";
        
        memoryCache.Set(cacheKey, account.UserId, TimeSpan.FromMinutes(15));

        var resetLink = $"https://localhost:5032/Account/ResetPassword?token={token}";
        var message = $@"<p>Xin chào,</p><p>Bạn đã yêu cầu đặt lại mật khẩu. Vui lòng click vào link sau để đặt lại mật khẩu (có hiệu lực trong 15 phút):</p><p><a href='{resetLink}'>{resetLink}</a></p>";

        await emailService.SendEmailAsync(account.Email, "Đặt lại mật khẩu WearWhatever", message);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(string token, string newPassword)
    {
        var cacheKey = $"ResetToken_{token}";
        if (!memoryCache.TryGetValue(cacheKey, out int accountId))
        {
            return (false, "Link đặt lại mật khẩu đã hết hạn hoặc không hợp lệ.");
        }

        var account = await dbContext.Accounts.FindAsync(accountId);
        if (account is null) return (false, "Tài khoản không tồn tại.");

        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        account.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync();
        
        // Invalidate token
        memoryCache.Remove(cacheKey);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> GenerateEmailVerificationTokenAsync(string email)
    {
        var account = await accountRepository.GetByEmailAsync(email);
        if (account is null) return (false, "Tài khoản không tồn tại.");

        var token = Guid.NewGuid().ToString("N");
        var cacheKey = $"VerifyEmail_{token}";
        
        memoryCache.Set(cacheKey, account.UserId, TimeSpan.FromHours(24));

        var verifyLink = $"https://localhost:5032/Account/VerifyEmail?token={token}";
        var message = $@"<p>Xin chào,</p><p>Cảm ơn bạn đã đăng ký tài khoản tại WearWhatever. Vui lòng click vào link sau để xác minh địa chỉ email của bạn (có hiệu lực trong 24 giờ):</p><p><a href='{verifyLink}'>{verifyLink}</a></p>";

        await emailService.SendEmailAsync(account.Email, "Xác minh tài khoản WearWhatever", message);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> VerifyEmailAsync(string token)
    {
        var cacheKey = $"VerifyEmail_{token}";
        if (!memoryCache.TryGetValue(cacheKey, out int accountId))
        {
            return (false, "Link xác minh đã hết hạn hoặc không hợp lệ.");
        }

        // Add EmailVerified=true to the user's session if they are currently logged in?
        // Wait, verifying just means setting a flag. Since we can't use DB, we will store a persistent cache key or a claim.
        // Actually, let's store it in cache with infinite sliding expiration, or we just rely on cookies.
        // Since it's a light version, let's just say "Successfully verified". We won't block login, but we'll issue the claim during Login.
        // Wait, the prompt says "store verification state in memory OR claim-based flag".
        var verifiedKey = $"EmailVerified_{accountId}";
        memoryCache.Set(verifiedKey, true, new MemoryCacheEntryOptions { Priority = CacheItemPriority.NeverRemove });
        
        memoryCache.Remove(cacheKey);

        return (true, null);
    }
}
