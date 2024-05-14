using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using App.Models;
using Microsoft.AspNetCore.Authorization;
using App.Data;
using App.Utilities;

namespace App.Areas_Admin_Controllers
{
    [Authorize(Roles = RoleName.Administrator)]
    [Area("Admin")]
    [Route("Admin/Category/[action]")]
    public class CategoryController : Controller
    {
        private readonly AppDbContext _context;

        public CategoryController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Category
        public IActionResult Index()
        {
            var items = _context.Categories
                .Include(c => c.CategoryChildren)   // <-- Nạp các Category con
                .AsEnumerable()
                .Where(c => c.ParentCategory == null)
                .ToList();

            return View(items);
        }

        // GET: Admin/Category/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // GET: Admin/Category/Create
        public async Task<IActionResult> Create()
        {
            var listcategory = await _context.Categories.ToListAsync();
            listcategory.Insert(0, new Category()
            {
                Title = "Không có danh mục cha",
                Id = -1
            });
            ViewData["ParentId"] = new SelectList(await GetItemsSelectCategorie(), "Id", "Title", -1);
            return View();
        }

        async Task<IEnumerable<Category>> GetItemsSelectCategorie()
        {
            var items = await _context.Categories
                                .Include(c => c.CategoryChildren)
                                .Where(c => c.ParentCategory == null)
                                .ToListAsync();

            List<Category> resultitems = new List<Category>() {
                new Category() {
                    Id = -1,
                    Title = "Không có danh mục cha"
                }
            };
            Action<List<Category>, int> _ChangeTitleCategory = null;
            Action<List<Category>, int> ChangeTitleCategory = (items, level) =>
            {
                string prefix = String.Concat(Enumerable.Repeat("—", level));
                foreach (var item in items)
                {
                    item.Title = prefix + " " + item.Title;
                    resultitems.Add(item);
                    if ((item.CategoryChildren != null) && (item.CategoryChildren.Count > 0))
                    {
                        _ChangeTitleCategory(item.CategoryChildren.ToList(), level + 1);
                    }
                }
            };

            _ChangeTitleCategory = ChangeTitleCategory;
            ChangeTitleCategory(items, 0);

            return resultitems;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ParentId,Title,Content,Slug")] Category category)
        {
            // Generate and set the slug
            category.Slug = Utils.GenerateSlug($"{category.Title}-{category.Id}");


            // Check if the slug already exists
            bool slugExisted = await _context.Brands.AnyAsync(p => p.Slug == category.Slug);
            if (slugExisted)
            {
                ModelState.AddModelError("Slug", "Slug đã tồn tại trong cơ sở dữ liệu");
            }

            if (ModelState.IsValid)
            {
                if (category.ParentId == -1)
                    category.ParentId = null;
                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var listcategory = await _context.Categories.ToListAsync();
            listcategory.Insert(0, new Category()
            {
                Title = "Không có danh mục cha",
                Id = -1
            });
            ViewData["ParentId"] = new SelectList(await GetItemsSelectCategorie(), "Id", "Title", category.ParentId);
            return View(category);
        }

        // GET: Admin/Category/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Sử dụng AsNoTracking để tránh theo dõi thực thể nhiều lần
            var category = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            ViewData["ParentId"] = new SelectList(await GetItemsSelectCategorie(), "Id", "Title", category.ParentId);

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ParentId,Title,Content,Slug")] Category category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            if (category.ParentId == category.Id)
            {
                ModelState.AddModelError("ParentId", "Danh mục không thể cập nhật với chính nó làm mục cha.");
                ViewData["ParentId"] = new SelectList(await GetItemsSelectCategorie(), "Id", "Title", category.ParentId);
                return View(category);
            }
            if (await IsChildCategory(category.Id, category.ParentId))
            {
                ModelState.AddModelError("ParentId", "Danh mục không thể cập nhật với mục con làm mục cha.");
                ViewData["ParentId"] = new SelectList(await GetItemsSelectCategorie(), "Id", "Title", category.ParentId);
                return View(category);
            }
            if (ModelState.IsValid)
            {
                try
                {
                    // Sử dụng AsNoTracking để tránh lỗi
                    var originalCategory = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
                    if (originalCategory.Title != category.Title)
                    {
                        // Tên đã thay đổi, gọi hàm generate slug mới
                        category.Slug = Utils.GenerateSlug($"{category.Title}-{category.Id}");
                    }

                    if (category.ParentId == -1)
                    {
                        category.ParentId = null;
                    }

                    // Tách việc theo dõi thực thể cũ và cập nhật thực thể mới
                    _context.Entry(category).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ParentId"] = new SelectList(await GetItemsSelectCategorie(), "Id", "Title", category.ParentId);
            TempData["StatusMessage"] = "Đã cập nhật thành công danh mục.";  // Success message
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> IsChildCategory(int parentId, int? childId)
        {
            // Sử dụng AsNoTracking để tránh theo dõi thực thể nhiều lần
            var parentCategory = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == parentId);
            if (parentCategory != null)
            {
                // Kiểm tra xem danh mục chính nó hoặc các danh mục con có chứa childId không
                var allSubCategories = new List<Category> { parentCategory };
                await GetChildCategories(allSubCategories, parentCategory);
                if (allSubCategories.Any(c => c.Id == childId))
                {
                    return true;
                }
            }
            return false;
        }

        private async Task GetChildCategories(List<Category> allSubCategories, Category parentCategory)
        {
            // Sử dụng AsNoTracking để tránh theo dõi thực thể nhiều lần
            var children = await _context.Categories.AsNoTracking().Where(c => c.ParentId == parentCategory.Id).ToListAsync();
            if (children.Any())
            {
                allSubCategories.AddRange(children);
                foreach (var child in children)
                {
                    await GetChildCategories(allSubCategories, child);
                }
            }
        }


        // GET: Admin/Category/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            // Check if Category has any related Products
            if (_context.ProductCategories.Any(p => p.CategoryId == id))
            {
                TempData["StatusMessage"] = "Không thể xoá vì có sản phẩm tồn tại trong danh mục hiện tại.";
                return RedirectToAction(nameof(Delete), new { id });  // Redirect to Delete with error.
            }

            // Check if Category has any sub categories
            if (_context.Categories.Any(c => c.ParentId == id))
            {
                TempData["StatusMessage"] = "Không thể xoá vì có danh mục con trong danh mục hiện tại.";
                return RedirectToAction(nameof(Delete), new { id });  // Redirect to Delete with error.
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Đã xóa thành công danh mục.";  // Success message
            return RedirectToAction(nameof(Index));
        }
        private bool HasChildCategories(Category category)
        {
            return _context.Categories.Any(c => c.ParentId == category.Id);
        }

        private bool HasProducts(Category category)
        {
            return _context.ProductCategories.Any(pc => pc.CategoryId == category.Id);
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }



    }

}
