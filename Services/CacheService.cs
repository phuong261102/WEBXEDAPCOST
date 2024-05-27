using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using App.Models;

public class CacheService
{
    private readonly IMemoryCache _cache;
    private readonly AppDbContext _context;

    public CacheService(IMemoryCache cache, AppDbContext context)
    {
        _cache = cache;
        _context = context;
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        const string keycacheCategories = "_listallcategories";
        if (!_cache.TryGetValue(keycacheCategories, out List<Category> categories))
        {
            categories = await _context.Categories
                .Include(c => c.CategoryChildren)
                .Where(c => c.ParentCategory == null)
                .ToListAsync();

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(300));
            _cache.Set(keycacheCategories, categories, cacheEntryOptions);
        }

        return categories;
    }

    public Category FindCategoryBySlug(List<Category> categories, string slug)
    {
        foreach (var c in categories)
        {
            if (c.Slug == slug) return c;
            var c1 = FindCategoryBySlug(c.CategoryChildren.ToList(), slug);
            if (c1 != null)
                return c1;
        }
        return null;
    }

    public async Task<List<Brand>> GetBrandsAsync()
    {
        const string keyCacheBrands = "_listAllBrands";
        if (!_cache.TryGetValue(keyCacheBrands, out List<Brand> brands))
        {
            brands = await _context.Brands.ToListAsync();
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(300));
            _cache.Set(keyCacheBrands, brands, cacheEntryOptions);
        }

        return brands;
    }
}
