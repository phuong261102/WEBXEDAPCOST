using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using App.Data;
using App.Utilities;
using XEDAPVIP.Models;
using App.Models;

namespace XEDAPVIP.Areas.Admin.Controllers
{
    [Authorize(Roles = RoleName.Administrator)]
    [Area("Admin")]
    [Route("Admin/Brand/[action]")]
    public class BrandController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public BrandController(AppDbContext context, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
        }

        // GET: Brand
        public async Task<IActionResult> Index([FromQuery(Name = "p")] int currentPage, int pagesize)
        {
            var items = _context.Brands.AsNoTracking().OrderByDescending(p => p.DateCreated);
            int totalItems = await items.CountAsync();
            if (pagesize <= 0) pagesize = 10;
            int countPages = (int)Math.Ceiling((double)totalItems / pagesize);
            if (currentPage > countPages)
                currentPage = countPages;
            if (currentPage < 1)
                currentPage = 1;
            var pagingmodel = new PagingModel()
            {
                countpages = countPages,
                currentpage = currentPage,
                generateUrl = (pageNumber) => Url.Action("Index", new
                {
                    p = pageNumber,
                    pagesize = pagesize
                })
            };
            ViewBag.pagingmodel = pagingmodel;
            ViewBag.totalProduc = totalItems;

            var iteminPage = await items.Skip((currentPage - 1) * pagesize)
                                              .Take(pagesize)
                                              .ToListAsync();

            return View(iteminPage);
        }

        // GET: Brand/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var brand = await _context.Brands.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
            if (brand == null)
            {
                return NotFound();
            }

            return View(brand);
        }

        // GET: Brand/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Brand/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Brand brand, IFormFile Image)
        {
            if (brand == null)
            {
                return NotFound("Brand not found");
            }

            // Generate and set the slug
            brand.Slug = Utils.GenerateSlug(brand.Name);

            // Check if the slug already exists
            bool slugExisted = await _context.Brands.AnyAsync(p => p.Slug == brand.Slug);
            if (slugExisted)
            {
                ModelState.AddModelError("Slug", "Slug đã tồn tại trong cơ sở dữ liệu");
            }

            if (ModelState.IsValid)
            {
                brand.DateCreated = DateTime.Now;
                brand.DateUpdated = DateTime.Now;

                // Save image if exists
                if (Image != null && Image.Length > 0)
                {
                    brand.Image = await SaveImageAsync(Image, brand.Slug);
                }

                _context.Add(brand);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(brand);
        }

        // GET: Brand/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var brand = await _context.Brands.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
            if (brand == null)
            {
                return NotFound();
            }
            return View(brand);
        }

        // POST: Brand/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Brand brand, IFormFile Image)
        {
            if (id != brand.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingBrand = await _context.Brands.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
                    if (existingBrand == null)
                    {
                        return NotFound();
                    }

                    existingBrand.Name = brand.Name;
                    existingBrand.Slug = Utils.GenerateSlug(brand.Name);
                    existingBrand.Content = brand.Content;
                    existingBrand.DateUpdated = DateTime.Now;

                    // Save image if exists
                    if (Image != null && Image.Length > 0)
                    {
                        existingBrand.Image = await SaveImageAsync(Image, existingBrand.Slug);
                    }

                    _context.Update(existingBrand);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BrandExists(brand.Id))
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
            return View(brand);
        }

        // GET: Brand/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var brand = await _context.Brands.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
            if (brand == null)
            {
                return NotFound();
            }

            return View(brand);
        }

        // POST: Brand/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var brand = await _context.Brands.FindAsync(id);

            // Check if any products reference this brand
            var isBrandInUse = _context.Products.Any(p => p.BrandId == id);
            if (isBrandInUse)
            {
                TempData["FailureMessage"] = "Brand is being used by some products and cannot be deleted!";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            // If no products reference the brand, delete it
            if (brand != null)
            {
                _context.Brands.Remove(brand);
                await _context.SaveChangesAsync();
            }
            TempData["SuccessMessage"] = "Brand deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private bool BrandExists(int id)
        {
            return _context.Brands.Any(e => e.Id == id);
        }

        private async Task<string> SaveImageAsync(IFormFile image, string slug)
        {
            var directoryPath = Path.Combine(_hostingEnvironment.WebRootPath, "images/Brands", slug);
            Directory.CreateDirectory(directoryPath);

            var filePath = Path.Combine(directoryPath, image.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }
            return $"/images/Brands/{slug}/{image.FileName}";
        }
    }
}
