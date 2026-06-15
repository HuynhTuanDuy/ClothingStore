using System.Security.Claims;
using ClothingStore.Models.Entities;
using ClothingStore.Models.ViewModels;
using ClothingStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClothingStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DiscountProgramsController(IDiscountProgramService discountProgramService) : Controller
{
    private int CurrentUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    // ── GET /Admin/DiscountPrograms ──────────────────────────────────────
    public async Task<IActionResult> Index([FromQuery] DiscountProgramFilter filter)
    {
        var (programs, stats) = await discountProgramService.GetProgramsFilteredAsync(filter);
        ViewBag.Stats = stats;
        ViewBag.CurrentFilter = filter;
        return View(programs);
    }

    // ── GET /Admin/DiscountPrograms/Create ───────────────────────────────
    public IActionResult Create()
    {
        return View(new DiscountProgramEditViewModel
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(1),
            IsActive = true
        });
    }

    // ── POST /Admin/DiscountPrograms/Create ──────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DiscountProgramEditViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        if (model.EndDate <= model.StartDate)
        {
            ModelState.AddModelError(nameof(model.EndDate), "Ngày kết thúc phải sau ngày bắt đầu.");
            return View(model);
        }

        var program = new DiscountProgram
        {
            ProgramName = model.ProgramName,
            DiscountPercent = model.DiscountPercent,
            StartDate = model.StartDate.ToUniversalTime(),
            EndDate = model.EndDate.ToUniversalTime(),
            IsActive = model.IsActive
        };

        try
        {
            var saved = await discountProgramService.SaveProgramAsync(program, CurrentUserId);
            if (!saved)
            {
                ModelState.AddModelError(string.Empty, "Không thể lưu chương trình giảm giá.");
                return View(model);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }

        TempData["Success"] = "Tạo chương trình giảm giá thành công.";
        return RedirectToAction(nameof(Index));
    }

    // ── GET /Admin/DiscountPrograms/Edit/{id} ────────────────────────────
    public async Task<IActionResult> Edit(int id)
    {
        var model = await discountProgramService.GetProgramByIdAsync(id);
        if (model is null) return NotFound();
        
        // Convert to local time for display
        model.StartDate = model.StartDate.ToLocalTime();
        model.EndDate = model.EndDate.ToLocalTime();
        
        return View(model);
    }

    // ── POST /Admin/DiscountPrograms/Edit ────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(DiscountProgramEditViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        if (model.EndDate <= model.StartDate)
        {
            ModelState.AddModelError(nameof(model.EndDate), "Ngày kết thúc phải sau ngày bắt đầu.");
            return View(model);
        }

        var program = new DiscountProgram
        {
            ProgramID = model.ProgramID,
            ProgramName = model.ProgramName,
            DiscountPercent = model.DiscountPercent,
            StartDate = model.StartDate.ToUniversalTime(),
            EndDate = model.EndDate.ToUniversalTime(),
            IsActive = model.IsActive,
            RowVersion = model.RowVersion
        };

        try
        {
            var saved = await discountProgramService.SaveProgramAsync(program, CurrentUserId);
            if (!saved)
            {
                ModelState.AddModelError(string.Empty, "Chương trình giảm giá không tồn tại.");
                return View(model);
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(string.Empty, "Chương trình giảm giá đã được cập nhật bởi người dùng khác. Vui lòng tải lại trang trước khi tiếp tục chỉnh sửa.");
            return View(model);
        }
        catch (UnauthorizedAccessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }

        TempData["Success"] = "Cập nhật chương trình giảm giá thành công.";
        return RedirectToAction(nameof(Index));
    }

    // ── POST /Admin/DiscountPrograms/Toggle ──────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        try
        {
            await discountProgramService.ToggleProgramAsync(id, CurrentUserId);
            TempData["Success"] = "Đã thay đổi trạng thái chương trình giảm giá.";
        }
        catch (UnauthorizedAccessException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
