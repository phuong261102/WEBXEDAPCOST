using App.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using XEDAPVIP.Models;
using Microsoft.AspNetCore.Identity;

namespace App.Areas.Home.Controllers
{
    [Area("Home")]
    public class ProductViewController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProductViewController> _logger;
        private readonly UserManager<AppUser> _userManager;
        private readonly CacheService _cacheService;
        private readonly CartService _cartService;

        public ProductViewController(AppDbContext context, ILogger<ProductViewController> logger, IMemoryCache cache,
        CacheService cacheService, CartService cartService, UserManager<AppUser> userManager)
        {
            _context = context;
            _logger = logger;
            _cacheService = cacheService;
            _cartService = cartService;
            _userManager = userManager;

        }



        [Route("/product/{categoryslug?}")]
        public async Task<IActionResult> Product(string categoryslug, string brandslug, [FromQuery(Name = "p")] int currentPage, int pagesize)
        {
            var categories = await _cacheService.GetCategoriesAsync();
            var brands = await _cacheService.GetBrandsAsync();

            ViewBag.categories = categories;
            ViewBag.categoryslug = categoryslug;
            ViewBag.brands = brands;
            ViewBag.brandslug = brandslug;

            Category category = null;

            if (!string.IsNullOrEmpty(categoryslug))
            {
                category = await _context.Categories
                    .Where(c => c.Slug == categoryslug)
                    .Include(c => c.CategoryChildren)
                    .FirstOrDefaultAsync();

                if (category == null)
                {
                    return NotFound("Không tìm thấy");
                }
            }

            var products = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .Include(p => p.ProductCategories)
                    .ThenInclude(pc => pc.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(brandslug))
            {
                products = products.Where(p => p.Brand.Slug == brandslug);
            }

            if (category != null)
            {
                products = products.Where(p => p.ProductCategories.Any(pc => pc.CategoryId == category.Id));
            }

            products = products.OrderByDescending(p => p.DateCreated);

            int totalProducts = await products.CountAsync();
            if (pagesize <= 0) pagesize = 9;
            int countPages = (int)Math.Ceiling((double)totalProducts / pagesize);
            if (currentPage > countPages)
                currentPage = countPages;
            if (currentPage < 1)
                currentPage = 1;

            var pagingmodel = new PagingModel()
            {
                countpages = countPages,
                currentpage = currentPage,
                generateUrl = (pageNumber) => Url.Action("Product", new
                {
                    categoryslug,
                    brandslug,
                    p = pageNumber,
                    pagesize
                })
            };

            var productinPage = await products.Skip((currentPage - 1) * pagesize)
                                        .Take(pagesize)
                                        .ToListAsync();

            ViewBag.pagingmodel = pagingmodel;
            ViewBag.totalProduc = totalProducts;
            ViewBag.category = category;
            return View(productinPage);
        }

        [Route("/product/{categoryslug}/{productslug}.cshtml")]
        public async Task<IActionResult> DetailProduct(string categoryslug, string brandslug, string productslug)
        {
            var categories = await _cacheService.GetCategoriesAsync();
            var brands = await _cacheService.GetBrandsAsync();

            ViewBag.categories = categories;
            ViewBag.categoryslug = categoryslug;
            ViewBag.brands = brands;
            ViewBag.brandslug = brandslug;

            var product = await _context.Products
                .Where(p => p.Slug == productslug)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .Include(p => p.ProductCategories)
                    .ThenInclude(pc => pc.Category)
                .FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound("Không tìm thấy sản phẩm");
            }

            ViewBag.product = product;
            return View(product);
        }
        [HttpPost]
        [Route("addcart/{productId:int}")]
        public async Task<IActionResult> AddToCart(int productId, [FromQuery] int productCode, [FromBody] CartItem cartItem)
        {
            var productVariant = _context.productVariants
                .FirstOrDefault(p => p.Id == productCode && p.ProductId == productId);

            if (productVariant == null)
            {
                return NotFound("Product variant not found");
            }

            var user = await GetCurrentUserAsync();
            string userId = user?.Id;  // Get the user ID if authenticated

            List<CartItem> cart;
            if (!string.IsNullOrEmpty(userId))
            {
                cart = _cartService.GetCartItems(userId);
            }
            else
            {
                cart = _cartService.GetCartItems();
            }

            var existingCartItem = cart.FirstOrDefault(ci => ci.Variant.Id == productCode);

            if (existingCartItem != null)
            {
                existingCartItem.Quantity += cartItem.Quantity;
            }
            else
            {
                cartItem.Variant = productVariant;
                cartItem.UserId = userId; // Assign userId (may be null for guests)
                cart.Add(cartItem);
            }

            _cartService.SaveCartItems(userId, cart);

            return Ok(new { message = "Item added to cart successfully." });
        }


        // Hiện thị giỏ hàng
        [Route("/cart", Name = "cart")]
        public async Task<IActionResult> Cart()
        {
            var categories = await _cacheService.GetCategoriesAsync();
            var brands = await _cacheService.GetBrandsAsync();

            ViewBag.categories = categories;
            ViewBag.brands = brands;
            var user = await GetCurrentUserAsync();
            string userId = user?.Id;  // Get the user ID if authenticated

            List<CartItem> cart;

            if (!string.IsNullOrEmpty(userId))
            {
                cart = _cartService.GetCartItems(userId);
            }
            else
            {
                cart = _cartService.GetCartItems();
            }
            return View(cart);
        }



        private Task<AppUser> GetCurrentUserAsync()
        {
            return _userManager.GetUserAsync(HttpContext.User);
        }

    }
}
