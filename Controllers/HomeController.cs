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
using System.Diagnostics;
using XEDAPVIP.Models;
using App.Components;
using Microsoft.Extensions.Caching.Memory;

namespace App.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<HomeController> _logger;
    // private IMemoryCache _cache;
    private readonly CacheService _cacheService;

    public HomeController(ILogger<HomeController> logger,
            AppDbContext context,
            IMemoryCache cache,
            CacheService cacheService)
    {
        _logger = logger;
        _context = context;
        _cacheService = cacheService;
    }




    private async Task<List<Product>> GetProductsByCategorySlugAsync(string categorySlug, int maxProducts)
    {
        return await _context.Products
            .Where(p => p.ProductCategories.Any(pc => pc.Category.Slug == categorySlug))
            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
            .Take(maxProducts) // Giới hạn số lượng sản phẩm
            .ToListAsync();
    }
    private async Task<List<Product>> GetNewestProductsAsync(int maxProducts)
    {
        return await _context.Products
            .OrderByDescending(p => p.DateCreated) // Sắp xếp theo ngày tạo để lấy sản phẩm mới nhất
            .Take(maxProducts)
            .ToListAsync();
    }

    public async Task<IActionResult> Index(string categoryslug, string brandslug)
    {
        var categories = await _cacheService.GetCategoriesAsync();
        var brands = await _cacheService.GetBrandsAsync();

        ViewBag.categories = categories;
        ViewBag.categoryslug = categoryslug;
        ViewBag.brands = brands;
        ViewBag.brandslug = brandslug;


        ViewBag.productsBaBy = await GetProductsByCategorySlugAsync("xe-dap-tre-em-0", 8);
        ViewBag.productsDua = await GetProductsByCategorySlugAsync("xe-dap-dua-0", 8);
        ViewBag.productsDuongpho = await GetProductsByCategorySlugAsync("xe-dap-the-thao-duong-pho-0", 8);
        ViewBag.productsPhuNu = await GetProductsByCategorySlugAsync("xe-dap-nu-0", 8);
        ViewBag.productsdiahinh = await GetProductsByCategorySlugAsync("xe-dap-dia-hinh-0", 8);
        ViewBag.productsgap = await GetProductsByCategorySlugAsync("xe-dap-gap-0", 8);


        ViewBag.productsNew = await GetNewestProductsAsync(4);
        return View();
    }



    public async Task<IActionResult> Privacy(string categoryslug, string brandslug)
    {
        var categories = await _cacheService.GetCategoriesAsync();
        var brands = await _cacheService.GetBrandsAsync();

        ViewBag.categories = categories;
        ViewBag.categoryslug = categoryslug;
        ViewBag.brands = brands;
        ViewBag.brandslug = brandslug;
        return View();
    }

    private async Task<List<Product>> GetSaleProductsByCategorySlugAsyncSale(string categorySlug, int maxProducts)
    {
        return await _context.Products
            .Where(p => p.ProductCategories.Any(pc => pc.Category.Slug == categorySlug) && p.DiscountPrice.HasValue)
            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
            .Take(maxProducts)
            .ToListAsync();
    }

    private async Task<List<Product>> GetTopSaleProductsAsync(int maxProducts)
    {
        return await _context.Products
            .Where(p => p.DiscountPrice.HasValue)
            .OrderByDescending(p => (p.Price - p.DiscountPrice.Value) / p.Price) // Tính phần trăm giảm giá và sắp xếp giảm dần
            .Take(maxProducts)
            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
            .ToListAsync();
    }

    public async Task<IActionResult> Sale(string categoryslug, string brandslug)
    {
        var categories = await _cacheService.GetCategoriesAsync();
        var brands = await _cacheService.GetBrandsAsync();
        ViewBag.categories = categories;
        ViewBag.categoryslug = categoryslug;
        ViewBag.brands = brands;
        ViewBag.brandslug = brandslug;

        ViewBag.productsBaBysale = await GetSaleProductsByCategorySlugAsyncSale("xe-dap-tre-em-0", 8);
        ViewBag.productsDuasale = await GetSaleProductsByCategorySlugAsyncSale("xe-dap-dua-0", 8);
        ViewBag.productsDuongphosale = await GetSaleProductsByCategorySlugAsyncSale("xe-dap-the-thao-duong-pho-0", 8);
        ViewBag.productsPhuNusale = await GetSaleProductsByCategorySlugAsyncSale("xe-dap-nu-0", 8);
        ViewBag.productsdiahinhsale = await GetSaleProductsByCategorySlugAsyncSale("xe-dap-dia-hinh-0", 8);
        ViewBag.productsgapsale = await GetSaleProductsByCategorySlugAsyncSale("xe-dap-gap-0", 8);

        ViewBag.topSaleProducts = await GetTopSaleProductsAsync(8);
        ViewBag.topSalehotProducts = await GetTopSaleProductsAsync(4);
        return View();
    }



    public async Task<IActionResult> Service(string categoryslug, string brandslug)
    {
        var categories = await _cacheService.GetCategoriesAsync();
        var brands = await _cacheService.GetBrandsAsync();

        ViewBag.categories = categories;
        ViewBag.categoryslug = categoryslug;
        ViewBag.brands = brands;
        ViewBag.brandslug = brandslug;
        return View();
    }
    public async Task<IActionResult> Cart(string categoryslug, string brandslug)
    {
        var categories = await _cacheService.GetCategoriesAsync();
        var brands = await _cacheService.GetBrandsAsync(); ;

        ViewBag.categories = categories;
        ViewBag.categoryslug = categoryslug;
        ViewBag.brands = brands;
        ViewBag.brandslug = brandslug;
        return View();
    }




    public IActionResult Address_shop()
    {
        return View();
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
