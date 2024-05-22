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
using Newtonsoft.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using XEDAPVIP.Areas.Admin.ModelsProduct;

namespace XEDAPVIP.Areas.Admin.Controllers
{
    [Authorize(Roles = RoleName.Administrator)]
    [Area("Admin")]
    [Route("Admin/Product/[action]")]
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public ProductController(AppDbContext context, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
        }

        // GET: Product
        public async Task<IActionResult> Index([FromQuery(Name = "p")] int currentPage, int pagesize)
        {
            var products = _context.Products.OrderByDescending(p => p.DateUpdated);
            var productCount = products.Count();
            ViewBag.countproduct = productCount;
            int totalProduc = await products.CountAsync();
            if (pagesize <= 0) pagesize = 12;
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
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .Include(p => p.ProductCategories)
                    .ThenInclude(pc => pc.Category)
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

            var brands = await _context.Brands.ToListAsync();
            ViewBag.brands = new SelectList(brands, "Id", "Name");

            var productDetails = new List<ProductDetailEntry>();
            productDetails.Add(new ProductDetailEntry());

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
            if (product.CategoryId == null || product.CategoryId.Length == 0)
            {
                TempData["StatusMessage"] = "Phải chọn ít nhất một danh mục";
                return await ReinitializeCreateView(product);
            }

            if (product.Variants == null || !product.Variants.Any())
            {
                TempData["StatusMessage"] = "Phải nhập ít nhất một biến thể";
                return await ReinitializeCreateView(product);
            }

            if (mainImage == null || mainImage.Length == 0)
            {
                TempData["StatusMessage"] = "Phải tải lên ảnh chính cho sản phẩm";
                return await ReinitializeCreateView(product);
            }

            product.Slug = Utils.GenerateSlug(product.Name);
            ModelState.SetModelValue("Slug", new ValueProviderResult(product.Slug));

            ModelState.Clear();
            TryValidateModel(product);

            if (!ModelState.IsValid)
            {
                return await ReinitializeCreateView(product);
            }

            bool SlugExisted = await _context.Products.AnyAsync(p => p.Slug == product.Slug);
            if (SlugExisted)
            {
                TempData["StatusMessage"] = "Slug đã tồn tại trong cơ sở dữ liệu";
                return await ReinitializeCreateView(product);
            }

            var productDetails = new Dictionary<string, string>();
            foreach (var detail in product.ProductDetails)
            {
                if (string.IsNullOrEmpty(detail.DetailsName) || string.IsNullOrEmpty(detail.DetailsValue))
                {
                    TempData["StatusMessage"] = "Chi tiết sản phẩm không được để trống.";
                    return await ReinitializeCreateView(product);
                }
                productDetails.Add(detail.DetailsName, detail.DetailsValue);
            }
            var detailsJson = JsonConvert.SerializeObject(productDetails);

            try
            {
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
                    Variants = product.Variants.Select(v => new ProductVariant { Color = v.Color, Size = v.Size, Quantity = v.Quantity }).ToList(),
                    BrandId = product.BrandId
                };

                if (mainImage != null && mainImage.Length > 0)
                {
                    var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), $"wwwroot/images/products/{newProduct.Slug}");
                    Directory.CreateDirectory(directoryPath);

                    var path = Path.Combine(directoryPath, mainImage.FileName);
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await mainImage.CopyToAsync(stream);
                    }
                    newProduct.MainImage = mainImage.FileName;
                }

                if (subImages != null && subImages.Count > 0)
                {
                    newProduct.SubImages = new List<string>();
                    int imageCount = 1;
                    foreach (var image in subImages)
                    {
                        if (image.Length > 0)
                        {
                            var fileDirectoryName = Path.Combine(Directory.GetCurrentDirectory(), $"wwwroot/images/products/{newProduct.Slug}/subImg");
                            Directory.CreateDirectory(fileDirectoryName);

                            var fileName = $"{newProduct.Slug}_{newProduct.Id}_{imageCount}{Path.GetExtension(image.FileName)}";
                            var filePath = Path.Combine(fileDirectoryName, fileName);
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await image.CopyToAsync(stream);
                            }
                            newProduct.SubImages.Add(fileName);
                            imageCount++;
                        }
                    }
                }

                _context.Products.Add(newProduct);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Sản phẩm đã được tạo thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception e)
            {
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi tạo sản phẩm. Vui lòng thử lại.");
            }

            return await ReinitializeCreateView(product);
        }

        // GET: Admin/Product/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var product = await _context.Products.Where(p => p.Id == id)
                    .Include(p => p.Variants)
                    .Include(p => p.Brand)
                    .Include(p => p.ProductCategories)
                    .ThenInclude(c => c.Category).FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound();
            }

            ViewBag.BrandList = new SelectList(_context.Brands, "Id", "Name");
            var selectedCates = product.ProductCategories.Select(c => c.CategoryId).ToArray();
            var categories = await _context.Categories.ToListAsync();
            ViewData["categories"] = new MultiSelectList(categories, "Id", "Title");

            var model = new CreateProductModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                DiscountPrice = product.DiscountPrice,
                Slug = product.Slug,
                MainImage = product.MainImage,
                SubImages = product.SubImages,
                BrandId = product.BrandId,
                CategoryId = product.ProductCategories.Select(pc => pc.CategoryId).ToArray(),
                Variants = product.Variants.Select(v => new ProductVariant
                {
                    Color = v.Color,
                    Size = v.Size,
                    Quantity = v.Quantity
                }).ToList(),
                DetailsDictionary = product.DetailsDictionary
            };

            return View(model);
        }

        // POST: Admin/Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateProductModel product, IFormFile mainImage, List<IFormFile> subImages, List<ProductVariant> variants, string existingSubImages)
        {
            var categories = await _context.Categories.ToListAsync();
            ViewData["categories"] = new MultiSelectList(categories, "Id", "Title");
            if (id != product.Id)
            {
                return NotFound();
            }
            product.Slug = Utils.GenerateSlug(product.Name);
            ModelState.SetModelValue("Slug", new ValueProviderResult(product.Slug));

            ModelState.Clear();
            TryValidateModel(product);

            if (ModelState.IsValid)
            {
                try
                {
                    var existingProduct = await _context.Products.Where(p => p.Id == id)
                        .Include(p => p.Variants)
                        .Include(p => p.Brand)
                        .Include(p => p.ProductCategories)
                        .ThenInclude(c => c.Category)
                        .FirstOrDefaultAsync();

                    if (existingProduct == null)
                    {
                        return NotFound();
                    }

                    existingProduct.Name = product.Name;
                    existingProduct.Description = product.Description;
                    existingProduct.Price = product.Price;
                    existingProduct.DiscountPrice = product.DiscountPrice;
                    existingProduct.BrandId = product.BrandId;
                    existingProduct.Slug = product.Slug;
                    existingProduct.DateUpdated = DateTime.Now;

                    // Update categories
                    if (product.CategoryId == null) product.CategoryId = new int[] { };
                    var oldCategoriesId = existingProduct.ProductCategories.Select(c => c.CategoryId).ToArray();
                    var newCategoriesId = product.CategoryId;

                    var removeCategories = from productCate in existingProduct.ProductCategories
                                           where !newCategoriesId.Contains(productCate.CategoryId)
                                           select productCate;
                    _context.ProductCategories.RemoveRange(removeCategories);

                    var addCateId = from cateIDs in newCategoriesId
                                    where !oldCategoriesId.Contains(cateIDs)
                                    select cateIDs;

                    foreach (var cateId in addCateId)
                    {
                        _context.ProductCategories.Add(new ProductCategory
                        {
                            ProductId = id,
                            CategoryId = cateId
                        });
                    }

                    // Update product variants
                    UpdateProductVariants(existingProduct, variants);

                    // Update product details
                    var detailsDictionary = product.ProductDetails.ToDictionary(d => d.DetailsName, d => d.DetailsValue);
                    existingProduct.DetailsJson = JsonConvert.SerializeObject(detailsDictionary);

                    // Update main image
                    if (mainImage != null && mainImage.Length > 0)
                    {
                        UpdateMainImage(existingProduct, mainImage);
                    }

                    // Update sub images
                    var existingSubImagesList = existingSubImages?.Split(',').ToList() ?? new List<string>();
                    UpdateSubImages(existingProduct, subImages, existingSubImagesList);

                    _context.Update(existingProduct);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Sản phẩm đã được cập nhật thành công.";
                    return RedirectToAction(nameof(Index));
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
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Đã xảy ra lỗi khi cập nhật sản phẩm: {ex.Message}");
                }
            }

            ViewBag.BrandList = new SelectList(_context.Brands, "Id", "Name", product.BrandId);
            ViewData["categories"] = new MultiSelectList(categories, "Id", "Title", product.CategoryId);

            return View(product);
        }

        // Update sub images
        private void UpdateSubImages(Product product, List<IFormFile> newSubImages, List<string> existingSubImages)
        {
            product.SubImages = new List<string>();

            // Add existing sub images to the product
            foreach (var existingImage in existingSubImages)
            {
                product.SubImages.Add(existingImage);
            }

            // Add new sub images to the product
            foreach (var image in newSubImages)
            {
                if (image.Length > 0)
                {
                    var fileDirectoryName = Path.Combine(_hostingEnvironment.WebRootPath, "images", "products", product.Slug, "subImg");
                    Directory.CreateDirectory(fileDirectoryName);

                    var fileName = $"{product.Slug}_{product.Id}_{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                    var filePath = Path.Combine(fileDirectoryName, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        image.CopyTo(stream);
                    }
                    product.SubImages.Add(fileName);
                }
            }
        }
        // Update product variants
        private void UpdateProductVariants(Product existingProduct, List<ProductVariant> newVariants)
        {
            // Get existing variants from database
            var existingVariants = _context.productVariants.Where(v => v.ProductId == existingProduct.Id).ToList();

            // Create a list to track variants to remove
            var variantsToRemove = existingVariants.Where(ev => !newVariants.Any(nv => nv.Id == ev.Id)).ToList();
            _context.productVariants.RemoveRange(variantsToRemove);

            // Update or add new variants
            foreach (var newVariant in newVariants)
            {
                var existingVariant = existingVariants.FirstOrDefault(ev => ev.Id == newVariant.Id);

                if (existingVariant != null)
                {
                    // Update existing variant
                    existingVariant.Color = newVariant.Color;
                    existingVariant.Size = newVariant.Size;
                    existingVariant.Quantity = newVariant.Quantity;
                }
                else
                {
                    // Add new variant
                    existingProduct.Variants.Add(newVariant);
                }
            }
        }
        // Update main image
        private void UpdateMainImage(Product product, IFormFile mainImage)
        {
            try
            {
                if (mainImage != null && mainImage.Length > 0)
                {
                    var directoryPath = Path.Combine(_hostingEnvironment.WebRootPath, "images", "products", product.Slug);
                    Directory.CreateDirectory(directoryPath);

                    // Generate a unique file name to prevent conflicts
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(mainImage.FileName);
                    var filePath = Path.Combine(directoryPath, uniqueFileName);

                    // Delete the old main image if it exists
                    var oldImagePath = Path.Combine(directoryPath, product.MainImage);
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }

                    // Save the new main image
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        mainImage.CopyTo(stream);
                    }

                    // Update the product's main image property
                    product.MainImage = uniqueFileName;
                }
            }
            catch (Exception ex)
            {
                // Handle the exception, log it, or display an error message
                ModelState.AddModelError(string.Empty, $"An error occurred while updating the main image: {ex.Message}");
            }
        }




        // Reinitialize edit view
        private async Task<IActionResult> ReinitializeEditView(int? id, Product product)
        {
            if (id == null)
            {
                return NotFound();
            }

            var brands = await _context.Brands.ToListAsync();
            ViewBag.BrandList = new SelectList(brands, "Id", "Name", product.BrandId);

            var categories = await _context.Categories.ToListAsync();
            ViewData["categories"] = new MultiSelectList(categories, "Id", "Title", product.ProductCategories.Select(pc => pc.CategoryId));

            return View("Edit", product);
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }


        private async Task<IActionResult> ReinitializeCreateView(CreateProductModel product)
        {
            var categories = await _context.Categories.ToListAsync();
            ViewData["categories"] = new MultiSelectList(categories, "Id", "Title");

            var brands = await _context.Brands.ToListAsync();
            ViewBag.brands = new SelectList(brands, "Id", "Name");

            return View("Create", product);
        }

        // GET: Admin/Post/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.Where(p => p.Id == id)
                    .Include(p => p.Variants)
                    .Include(p => p.Brand)
                    .Include(p => p.ProductCategories)
                    .ThenInclude(c => c.Category).FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.Where(p => p.Id == id)
                  .Include(p => p.Variants)
                  .Include(p => p.Brand)
                  .Include(p => p.ProductCategories)
                  .ThenInclude(c => c.Category).FirstOrDefaultAsync();

            // Lấy thông tin đường dẫn tới file hình ảnh chính và các hình ảnh phụ
            var mainImagePath = product.MainImage;
            var subImagePaths = product.SubImages; // Giả sử hình ảnh phụ được lưu trong thuộc tính SubImages 

            // Xóa file hình ảnh chính và các hình ảnh phụ
            if (!string.IsNullOrEmpty(mainImagePath) && System.IO.File.Exists(mainImagePath))
            {
                System.IO.File.Delete(mainImagePath);
            }

            if (subImagePaths != null)
            {
                foreach (var imagePath in subImagePaths)
                {
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}