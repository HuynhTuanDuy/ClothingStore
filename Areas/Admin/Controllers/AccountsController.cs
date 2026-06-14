using ClothingStore.Data;
using ClothingStore.Models.Entities;
using ClothingStore.Models.ViewModels;
using ClothingStore.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClothingStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AccountsController(
    StoreDbContext context,
    IAccountRepository accountRepository,
    IUnitOfWork unitOfWork,
    ILogger<AccountsController> logger) : Controller
{
    private static readonly List<string> AllStatuses = [
        AccountStatus.Active,
        AccountStatus.Inactive,
        AccountStatus.Banned
    ];

    public async Task<IActionResult> Index(AdminAccountFilter filter)
    {
        var query = context.Accounts
            .Include(a => a.AccountRoles)
            .ThenInclude(ar => ar.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string s = filter.Search.Trim().ToLower();
            query = query.Where(a => a.UserName.ToLower().Contains(s) || a.Email.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            query = query.Where(a => a.Status == filter.Status);
        }

        if (!string.IsNullOrWhiteSpace(filter.Role))
        {
            query = query.Where(a => a.AccountRoles.Any(ar => ar.Role.Name == filter.Role));
        }

        int totalCount = await query.CountAsync();

        var accounts = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(a => new AdminAccountListItemViewModel
            {
                UserId = a.UserId,
                UserName = a.UserName,
                Email = a.Email,
                Status = a.Status,
                Roles = a.AccountRoles.Select(ar => ar.Role.Name).ToList(),
                LastLoginAt = a.LastLoginAt,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        var allRoles = await context.Roles.Select(r => r.Name).ToListAsync();
        var roleOptions = allRoles.Select(r => new SelectListItem(r, r, r == filter.Role)).ToList();
        roleOptions.Insert(0, new SelectListItem("Tất cả quyền", ""));

        var vm = new AdminAccountPageViewModel
        {
            Accounts = accounts,
            Filter = filter,
            TotalCount = totalCount,
            AvailableRoles = roleOptions
        };

        return View(vm);
    }

    public async Task<IActionResult> Create()
    {
        var roles = await context.Roles.ToListAsync();
        var vm = new AdminAccountCreateViewModel
        {
            AvailableRoles = roles.Select(r => new SelectListItem(r.Name, r.RoleId.ToString())).ToList()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminAccountCreateViewModel model)
    {
        if (model.SelectedRoleIds == null || !model.SelectedRoleIds.Any())
        {
            ModelState.AddModelError("SelectedRoleIds", "Vui lòng chọn ít nhất 1 quyền.");
        }

        if (ModelState.IsValid)
        {
            if (await accountRepository.UsernameExistsAsync(model.UserName))
            {
                ModelState.AddModelError("UserName", "Tên đăng nhập đã tồn tại.");
            }
            else if (await accountRepository.EmailExistsAsync(model.Email))
            {
                ModelState.AddModelError("Email", "Email đã tồn tại.");
            }
            else
            {
                var now = DateTime.UtcNow;
                var account = new Account
                {
                    UserName = model.UserName.Trim(),
                    Email = model.Email.Trim().ToLowerInvariant(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Status = model.Status,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    await accountRepository.AddAccountAsync(account);
                    await unitOfWork.SaveChangesAsync(); // Need to save to get UserId

                    foreach (var roleId in model.SelectedRoleIds)
                    {
                        account.AccountRoles.Add(new AccountRole { UserId = account.UserId, RoleId = roleId });
                    }

                    await unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = "Tạo tài khoản thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    logger.LogError(ex, "Lỗi khi tạo tài khoản");
                    ModelState.AddModelError("", "Đã xảy ra lỗi hệ thống khi tạo tài khoản.");
                }
            }
        }

        var roles = await context.Roles.ToListAsync();
        model.AvailableRoles = roles.Select(r => new SelectListItem(r.Name, r.RoleId.ToString())).ToList();
        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var account = await context.Accounts
            .Include(a => a.AccountRoles)
            .FirstOrDefaultAsync(a => a.UserId == id);

        if (account == null) return NotFound();

        var roles = await context.Roles.ToListAsync();
        var vm = new AdminAccountEditViewModel
        {
            UserId = account.UserId,
            UserName = account.UserName, // Display only
            Email = account.Email,
            Status = account.Status,
            SelectedRoleIds = account.AccountRoles.Select(ar => ar.RoleId).ToList(),
            RowVersion = account.RowVersion,
            AvailableRoles = roles.Select(r => new SelectListItem(r.Name, r.RoleId.ToString())).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdminAccountEditViewModel model)
    {
        if (id != model.UserId) return BadRequest();

        if (model.SelectedRoleIds == null || !model.SelectedRoleIds.Any())
        {
            ModelState.AddModelError("SelectedRoleIds", "Vui lòng chọn ít nhất 1 quyền.");
        }

        var account = await context.Accounts
            .Include(a => a.AccountRoles)
            .FirstOrDefaultAsync(a => a.UserId == id);

        if (account == null) return NotFound();

        // Check if email changed and exists (ignore self)
        if (account.Email != model.Email.Trim().ToLowerInvariant())
        {
            if (await context.Accounts.AnyAsync(a => a.Email == model.Email.Trim().ToLowerInvariant() && a.UserId != id))
            {
                ModelState.AddModelError("Email", "Email đã tồn tại.");
            }
        }

        if (ModelState.IsValid)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool isSelf = currentUserId == account.UserId.ToString();

            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            bool isRemovingAdminRole = adminRole != null && account.AccountRoles.Any(ar => ar.RoleId == adminRole.RoleId) && !model.SelectedRoleIds.Contains(adminRole.RoleId);

            // 1. Self Protection
            if (isSelf)
            {
                if (model.Status != AccountStatus.Active)
                {
                    TempData["Error"] = "Bạn không thể tự vô hiệu hóa tài khoản của chính mình.";
                    model.Status = AccountStatus.Active; // Revert
                }
                
                if (isRemovingAdminRole)
                {
                    TempData["Error"] = "Bạn không thể tự gỡ quyền Admin của chính mình.";
                    model.SelectedRoleIds.Add(adminRole!.RoleId); // Revert
                    isRemovingAdminRole = false;
                }

                if (!string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    TempData["Error"] = "Bạn không thể tự reset mật khẩu của chính mình tại đây. Vui lòng sử dụng chức năng Đổi mật khẩu cá nhân.";
                    model.NewPassword = null; // Revert
                }
            }

            // 2. Last Admin Protection
            if (isRemovingAdminRole || (model.Status != AccountStatus.Active && account.AccountRoles.Any(ar => ar.RoleId == adminRole?.RoleId)))
            {
                int adminCount = await context.AccountRoles.CountAsync(ar => ar.RoleId == adminRole!.RoleId && ar.Account.Status == AccountStatus.Active);
                if (adminCount <= 1)
                {
                    ModelState.AddModelError("", "Hệ thống phải luôn tồn tại ít nhất một tài khoản Admin đang hoạt động.");
                    var rolesRetry = await context.Roles.ToListAsync();
                    model.AvailableRoles = rolesRetry.Select(r => new SelectListItem(r.Name, r.RoleId.ToString())).ToList();
                    model.UserName = account.UserName;
                    return View(model);
                }
            }

            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var changedByName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown Admin";

                if (account.Status != model.Status)
                {
                    logger.LogInformation("ActionType = StatusChanged | TargetUserId = {TargetUserId} | OldStatus = {OldStatus} | NewStatus = {NewStatus} | ChangedBy = {ChangedBy} | ChangedAt = {ChangedAt}",
                        account.UserId, account.Status, model.Status, changedByName, DateTime.UtcNow);
                }

                account.Status = model.Status;
                account.Email = model.Email.Trim().ToLowerInvariant();
                account.UpdatedAt = DateTime.UtcNow;

                // Set original RowVersion to trigger concurrency check
                if (model.RowVersion != null)
                {
                    context.Entry(account).Property(a => a.RowVersion).OriginalValue = model.RowVersion;
                }

                // Password Reset logic with Audit
                if (!string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                    logger.LogInformation("ActionType = PasswordReset | TargetUserId = {TargetUserId} | ChangedBy = {ChangedBy} | ChangedAt = {ChangedAt}", 
                        account.UserId, changedByName, DateTime.UtcNow);
                }

                // Role Update Logic
                var oldRoles = account.AccountRoles.Select(ar => ar.RoleId).OrderBy(id => id).ToList();
                var newRoles = model.SelectedRoleIds.OrderBy(id => id).ToList();

                if (!oldRoles.SequenceEqual(newRoles))
                {
                    logger.LogInformation("ActionType = RolesChanged | TargetUserId = {TargetUserId} | OldRoleIds = {OldRoleIds} | NewRoleIds = {NewRoleIds} | ChangedBy = {ChangedBy} | ChangedAt = {ChangedAt}",
                        account.UserId, string.Join(",", oldRoles), string.Join(",", newRoles), changedByName, DateTime.UtcNow);

                    context.AccountRoles.RemoveRange(account.AccountRoles);
                    await context.SaveChangesAsync();

                    foreach (var roleId in model.SelectedRoleIds)
                    {
                        account.AccountRoles.Add(new AccountRole { UserId = account.UserId, RoleId = roleId });
                    }
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "Cập nhật tài khoản thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Tài khoản đã được cập nhật bởi người khác. Vui lòng tải lại trang để xem thông tin mới nhất.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "Lỗi cập nhật quyền tài khoản {UserId}", account.UserId);
                ModelState.AddModelError("", "Đã xảy ra lỗi hệ thống khi cập nhật tài khoản.");
            }
        }

        var rolesList = await context.Roles.ToListAsync();
        model.AvailableRoles = rolesList.Select(r => new SelectListItem(r.Name, r.RoleId.ToString())).ToList();
        model.UserName = account.UserName; // ensure it's repopulated for the view
        return View(model);
    }
}
