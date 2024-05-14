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

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            // Check if the category being edited is not a child of itself
            var categoryIds = await _context.Categories.Select(c => c.Id).ToListAsync();
            if (!categoryIds.Contains(category.ParentId ?? 0))
            {
                ViewData["ParentId"] = new SelectList(await GetItemsSelectCategorie(), "Id", "Title", category.ParentId);
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể cập nhật danh mục cha thành một danh mục con của nó.";
                return RedirectToAction(nameof(Index)); // Redirect back to the index page or any other appropriate page
            }

            return View(category);
        }

        // POST: Admin/Category/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ParentId,Title,Content,Slug")] Category category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (category.ParentId == -1)
                    {
                        category.ParentId = null;
                    }
                    _context.Update(category);
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
            var listcategory = await _context.Categories.ToListAsync();
            listcategory.Insert(0, new Category()
            {
                Title = "Không có danh mục cha",
                Id = -1
            });
            ViewData["ParentId"] = new SelectList(listcategory, "Id", "Title", category.ParentId);
            return View(category);
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

        // POST: Admin/Category/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            if (HasChildCategories(category))
            {
                TempData["ErrorMessage"] = "Không thể xóa danh mục có mục con.";
                return RedirectToAction(nameof(Index)); // Redirect back to the index page or any other appropriate page
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa danh mục thành công."; // Return a success message
            return RedirectToAction(nameof(Index)); // Redirect back to the index page or any other appropriate page
        }

        private bool HasChildCategories(Category category)
        {
            return _context.Categories.Any(c => c.ParentId == category.Id);
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}
