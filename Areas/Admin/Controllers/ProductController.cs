using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using App.Models;
using App.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using App.Utilities;
using XEDAPVIP.Areas.Admin.Models;
using Newtonsoft.Json;

namespace XEDAPVIP.Areas.Admin.Controllers
{
    [Authorize(Roles = RoleName.Administrator)]
    [Area("Admin")]
    [Route("Admin/Product/[action]")]
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Product
        public async Task<IActionResult> Index([FromQuery(Name = "p")] int currentPage, int pagesize)
        {
            var products = _context.Products.OrderByDescending(p => p.DateCreated);
            int totalProduc = await products.CountAsync();
            if (pagesize <= 0) pagesize = 10;
            int countPages = (int)Math.Ceiling((double)totalProduc / pagesize);
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
            ViewBag.totalProduc = totalProduc;

            var productinPage = await products.Skip((currentPage - 1) * pagesize)
                                              .Take(pagesize)
                                              .Include(p => p.ProductCategories)
                                              .ThenInclude(pc => pc.Category)
                                              .ToListAsync();

            return View(productinPage);
        }

        // GET: Product/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Product/Create
        [BindProperty]
        public int[] selectedCategories { set; get; }
        public async Task<IActionResult> Create()
        {
            var categories = await _context.Categories.ToListAsync();
            ViewData["categories"] = new MultiSelectList(categories, "Id", "Title");

            // Initialize an empty list of product details
            var productDetails = new List<ProductDetailEntry>();
            // Add a default product detail entry
            productDetails.Add(new ProductDetailEntry());
            // Assign the list of product details to the CreateProductModel
            var model = new CreateProductModel
            {
                ProductDetails = productDetails
            };

            return View(model);
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProductModel product, IFormFile mainImage, List<IFormFile> subImages)
        {
            // Kiểm tra và xử lý ModelState

            if (product.CategoryId == null || product.CategoryId.Length == 0)
            {
                TempData["StatusMessage"] = "Phải chọn ít nhất một danh mục";
            }

            if (product.Variants == null || !product.Variants.Any())
            {
                TempData["StatusMessage"] = "Phải nhập ít nhất một biến thể";
            }
            if (mainImage == null || mainImage.Length == 0)
            {
                TempData["StatusMessage"] = "Phải tải lên ảnh chính cho sản phẩm";
            }
            product.Slug = Utils.GenerateSlug(product.Name);
            ModelState.SetModelValue("Slug", new ValueProviderResult(product.Slug));

            // Thiết lập và kiểm tra lại Model
            ModelState.Clear();
            TryValidateModel(product);

            // Kiểm tra slug đã tồn tại hay chưa
            bool SlugExisted = await _context.Products.AnyAsync(p => p.Slug == product.Slug);
            if (SlugExisted)
            {
                TempData["StatusMessage"] = "Slug đã tồn tại trong cơ sở dữ liệu";
            }


            var productDetails = new Dictionary<string, string>();
            foreach (var detail in product.ProductDetails)
            {
                if (string.IsNullOrEmpty(detail.DetailsName) || string.IsNullOrEmpty(detail.DetailsValue))
                {
                    TempData["StatusMessage"] = "Chi tiết sản phẩm không được để trống.";
                    var categories = await _context.Categories.ToListAsync();
                    ViewData["categories"] = new MultiSelectList(categories, "Id", "Title", product.CategoryId);
                    return View(product);
                }
                productDetails.Add(detail.DetailsName, detail.DetailsValue);
            }
            var detailsJson = JsonConvert.SerializeObject(productDetails);

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

                foreach (var error in errors)
                {
                    System.Diagnostics.Debug.WriteLine("Key: " + error.Key);
                    foreach (var err in error.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine("Error Message: " + err.ErrorMessage);
                        if (err.Exception != null)
                        {
                            System.Diagnostics.Debug.WriteLine("Exception Message: " + err.Exception.Message);
                        }
                    }
                }

                var categories = await _context.Categories.ToListAsync();
                ViewData["categories"] = new MultiSelectList(categories, "Id", "Title", product.CategoryId);
                return View(product);
            }
            try
            {
                // Tạo mới sản phẩm
                var newProduct = new Product
                {
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    DiscountPrice = product.DiscountPrice,
                    Slug = product.Slug,
                    DetailsJson = detailsJson,
                    DateCreated = DateTime.Now,
                    DateUpdated = DateTime.Now,
                    ProductCategories = product.CategoryId.Select(catId => new ProductCategory { CategoryId = catId }).ToList(),
                    Variants = product.Variants.Select(v => new ProductVariant { Color = v.Color, Size = v.Size, Quantity = v.Quantity }).ToList()
                };
                var productSlug = newProduct.Slug;

                if (mainImage != null && mainImage.Length > 0)
                {
                    var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), $"wwwroot/images/products/{productSlug}");
                    Directory.CreateDirectory(directoryPath);  // create the directory if it doesn't exist

                    var path = Path.Combine(directoryPath, mainImage.FileName);
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await mainImage.CopyToAsync(stream);
                    }
                    newProduct.MainImage = mainImage.FileName;
                }

                // Same goes for SubImages:
                if (subImages != null && subImages.Count > 0)
                {
                    newProduct.SubImages = new List<string>();
                    foreach (var image in subImages)
                    {
                        if (image.Length > 0)
                        {
                            var fileDirectoryName = Path.Combine(Directory.GetCurrentDirectory(), $"wwwroot/images/products/{productSlug}/subImg");
                            Directory.CreateDirectory(fileDirectoryName);

                            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                            var filePath = Path.Combine(fileDirectoryName, fileName);
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await image.CopyToAsync(stream);
                            }
                            newProduct.SubImages.Add(fileName);
                        }
                    }
                }
                _context.Products.Add(newProduct);
                await _context.SaveChangesAsync();

                TempData["StatusMessage"] = "Sản phẩm đã được tạo thành công.";
                return RedirectToAction(nameof(Index)); // Chuyển hướng về danh sách sản phẩm
            }
            catch (Exception e)
            {
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi tạo sản phẩm. Vui lòng thử lại.");
            }

            // Nếu ModelState không hợp lệ, trả về view với thông tin sản phẩm và danh sách lựa chọn danh mục
            var categoriesList = await _context.Categories.ToListAsync();
            ViewData["categories"] = new MultiSelectList(categoriesList, "Id", "Title", product.CategoryId);
            return View(product);
        }

        // GET: Product/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,DiscountPrice,Slug,DateCreated,DateUpdated")] Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
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
            return View(product);
        }

        // GET: Product/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}
