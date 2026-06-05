using ClothingStore.Data;
using ClothingStore.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClothingStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoriesController : Controller
    {
        private readonly StoreDbContext _context;

        public CategoriesController(StoreDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Categories
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            // Validate parameters
            if (page < 1) page = 1;
            int[] allowedPageSizes = { 10, 25, 50, 100 };
            if (!allowedPageSizes.Contains(pageSize)) pageSize = 10;

            var query = _context.Categories
                .Include(c => c.Products)
                .Include(c => c.ParentCategory)
                .AsQueryable();

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var categories = await query
                .OrderByDescending(c => c.Products.Count)
                .ThenBy(c => c.CategoryName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            var allActiveCategories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
            ViewBag.ParentCategories = allActiveCategories.Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = c.CategoryID.ToString(),
                Text = c.CategoryName
            }).ToList();

            return View(categories);
        }

        // GET: Admin/Categories/Create
        public async Task<IActionResult> Create(int? parentId)
        {
            var model = new ClothingStore.Models.ViewModels.CategoryFormViewModel
            {
                IsActive = true,
                ParentCategoryID = parentId
            };

            await PopulateParentCategories(model);
            return View(model);
        }

        // POST: Admin/Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClothingStore.Models.ViewModels.CategoryFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var category = new Category
                {
                    CategoryName = model.CategoryName,
                    ParentCategoryID = model.ParentCategoryID,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đã thêm danh mục thành công.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateParentCategories(model);
            return View(model);
        }

        // POST: Admin/Categories/EditAjax/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAjax(int id, ClothingStore.Models.ViewModels.CategoryFormViewModel model)
        {
            if (id != model.CategoryID || !ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = "Dữ liệu không hợp lệ.", errors = errors });
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null) return Json(new { success = false, message = "Không tìm thấy danh mục." });

            // Circular reference check
            if (await IsCircularReference(id, model.ParentCategoryID))
            {
                return Json(new { success = false, message = "Lỗi: Không thể chọn danh mục con làm danh mục cha." });
            }

            category.CategoryName = model.CategoryName;
            category.ParentCategoryID = model.ParentCategoryID;
            category.IsActive = model.IsActive;
            category.UpdatedAt = DateTime.UtcNow;

            _context.Categories.Update(category);
            await _context.SaveChangesAsync();

            // Lấy tên parent để trả về UI
            string parentName = null;
            if (category.ParentCategoryID.HasValue)
            {
                var parent = await _context.Categories.FindAsync(category.ParentCategoryID.Value);
                parentName = parent?.CategoryName;
            }

            return Json(new { success = true, message = "Cập nhật danh mục thành công.", data = new {
                categoryName = category.CategoryName,
                parentCategoryId = category.ParentCategoryID,
                parentName = parentName,
                isActive = category.IsActive,
                updatedAt = category.UpdatedAt?.ToString("dd/MM/yyyy")
            }});
        }

        private async Task<bool> IsCircularReference(int categoryId, int? newParentId)
        {
            if (!newParentId.HasValue) return false;
            if (categoryId == newParentId.Value) return true;

            var currentParentId = newParentId.Value;
            while (true)
            {
                var parent = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryID == currentParentId);
                if (parent == null || !parent.ParentCategoryID.HasValue) return false;
                if (parent.ParentCategoryID.Value == categoryId) return true;
                currentParentId = parent.ParentCategoryID.Value;
            }
        }

        // POST: Admin/Categories/Archive/5
        [HttpPost]
        public async Task<IActionResult> Archive(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .Include(c => c.ChildCategories)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
            {
                return Json(new { success = false, message = "Không tìm thấy danh mục." });
            }

            if (category.Products.Any() || category.ChildCategories.Any())
            {
                // Soft delete
                category.IsActive = false;
                category.UpdatedAt = DateTime.UtcNow;
                _context.Categories.Update(category);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Danh mục đã được lưu trữ vì có chứa sản phẩm hoặc danh mục con." });
            }

            // If empty, we can choose to soft-delete anyway to keep history, as requested by user ("Không thực hiện Hard Delete")
            category.IsActive = false;
            category.UpdatedAt = DateTime.UtcNow;
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã lưu trữ danh mục thành công." });
        }

        // GET: Admin/Categories/ExportCsv
        public async Task<IActionResult> ExportCsv()
        {
            var categories = await _context.Categories
                .Include(c => c.Products)
                .Include(c => c.ParentCategory)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("CategoryID,CategoryName,ParentCategory,ProductCount,Status,CreatedAt,UpdatedAt");

            foreach (var cat in categories)
            {
                var parentName = cat.ParentCategory?.CategoryName ?? "";
                var productCount = cat.Products.Count;
                var status = cat.IsActive ? "Active" : "Archived";
                var createdAt = cat.CreatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                var updatedAt = cat.UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";

                // Escape commas in names
                var catName = $"\"{cat.CategoryName.Replace("\"", "\"\"")}\"";
                parentName = $"\"{parentName.Replace("\"", "\"\"")}\"";

                sb.AppendLine($"{cat.CategoryID},{catName},{parentName},{productCount},{status},{createdAt},{updatedAt}");
            }

            // UTF-8 with BOM
            var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", "CategoriesReport.csv");
        }

        private async Task PopulateParentCategories(ClothingStore.Models.ViewModels.CategoryFormViewModel model)
        {
            var categories = await _context.Categories
                .Where(c => c.IsActive && c.CategoryID != model.CategoryID) // prevent self-referencing
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            model.ParentCategories = categories.Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = c.CategoryID.ToString(),
                Text = c.CategoryName
            }).ToList();
        }
    }
}
