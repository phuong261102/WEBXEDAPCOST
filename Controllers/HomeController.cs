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




    public async Task<IActionResult> Index(string categoryslug, string brandslug)
    {
        var categories = await _cacheService.GetCategoriesAsync();
        var brands = await _cacheService.GetBrandsAsync();

        ViewBag.categories = categories;
        ViewBag.categoryslug = categoryslug;
        ViewBag.brands = brands;
        ViewBag.brandslug = brandslug;



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
    public async Task<IActionResult> Sale(string categoryslug, string brandslug)
    {
        var categories = await _cacheService.GetCategoriesAsync();
        var brands = await _cacheService.GetBrandsAsync();
        ViewBag.categories = categories;
        ViewBag.categoryslug = categoryslug;
        ViewBag.brands = brands;
        ViewBag.brandslug = brandslug;
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

    public async Task<IActionResult> Check_out(string categoryslug, string brandslug)
    {
        var categories = await _cacheService.GetCategoriesAsync();
        var brands = await _cacheService.GetBrandsAsync();

        ViewBag.categories = categories;
        ViewBag.categoryslug = categoryslug;
        ViewBag.brands = brands;
        ViewBag.brandslug = brandslug;
        return View();
    }

    public async Task<IActionResult> Product_select(string categoryslug, string brandslug)
    {
        var categories = await _cacheService.GetCategoriesAsync();
        var brands = await _cacheService.GetBrandsAsync();

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
