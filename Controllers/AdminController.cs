using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Data;
using App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace XEDAPVIP.Controllers
{
    [Authorize(Roles = RoleName.Administrator)]
    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;
        private readonly AppDbContext _context;

        public AdminController(ILogger<AdminController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var dashboardData = new DashboardData();

            // Lấy thông tin đơn hàng
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Variant)
                .ToListAsync();

            // Tính toán doanh thu
            var todayRevenue = await _context.Orders
                .Where(o => o.OrderDate.Date == DateTime.Today)
                .SumAsync(o => o.TotalAmount);

            var previousDayRevenue = await _context.Orders
                .Where(o => o.OrderDate.Date == DateTime.Today.AddDays(-1))
                .SumAsync(o => o.TotalAmount);

            var previousWeekRevenue = await _context.Orders
                .Where(o => o.OrderDate >= DateTime.Today.AddDays(-7) && o.OrderDate < DateTime.Today)
                .SumAsync(o => o.TotalAmount);

            var previousMonthRevenue = await _context.Orders
                .Where(o => o.OrderDate >= DateTime.Today.AddMonths(-1) && o.OrderDate < DateTime.Today)
                .SumAsync(o => o.TotalAmount);

            dashboardData.TotalRevenue = todayRevenue;
            dashboardData.PercentageChangePreviousDay = previousDayRevenue != 0 ? (todayRevenue - previousDayRevenue) * 100 / previousDayRevenue : 0;
            dashboardData.PercentageChangePreviousWeek = previousWeekRevenue != 0 ? (todayRevenue - previousWeekRevenue) * 100 / previousWeekRevenue : 0;
            dashboardData.PercentageChangePreviousMonth = previousMonthRevenue != 0 ? (todayRevenue - previousMonthRevenue) * 100 / previousMonthRevenue : 0;

            // Tính toán số lượng sản phẩm bán chạy
            var productSalesQuery = _context.OrderDetails
                .GroupBy(od => new { od.VariantId, od.Variant.ProductId })
                .Select(g => new
                {
                    g.Key.VariantId,
                    g.Key.ProductId,
                    TotalQuantity = g.Sum(od => od.Quantity)
                })
                .OrderByDescending(g => g.TotalQuantity)
                .Take(5)
                .AsQueryable(); // Ensure IQueryable

            var productSalesWithNamesQuery = productSalesQuery
                .Join(_context.Products,
                      ps => ps.ProductId,
                      p => p.Id,
                      (ps, p) => new ProductSale
                      {
                          VariantId = ps.VariantId.Value,
                          ProductName = p.Name,
                          TotalQuantity = ps.TotalQuantity
                      })
                .AsQueryable(); // Ensure IQueryable

            var productSalesWithNames = await productSalesWithNamesQuery.ToListAsync();

            dashboardData.ProductSales = productSalesWithNames;

            // Get top selling brands
            var topSellingBrands = await _context.OrderDetails
                .Include(od => od.Variant.Product.Brand)
                .GroupBy(od => new { od.Variant.Product.Brand.Id, od.Variant.Product.Brand.Name })
                .Select(g => new BrandSale
                {
                    BrandId = g.Key.Id,
                    BrandName = g.Key.Name,
                    QuantitySold = g.Sum(od => od.Quantity)
                })
                .OrderByDescending(bs => bs.QuantitySold)
                .Take(5)
                .ToListAsync();
            dashboardData.TopSellingBrands = topSellingBrands;
            // Tính toán tổng số người dùng và đơn hàng
            dashboardData.TotalUsers = await _context.Users.CountAsync();
            dashboardData.TotalOrders = orders.Count;
            dashboardData.PendingOrders = orders.Count(o => o.Status == "Pending");
            dashboardData.PaidOrders = orders.Count(o => o.Status == "Paid");
            dashboardData.CompletedOrders = orders.Count(o => o.Status == "Completed");
            dashboardData.CanceledOrders = orders.Count(o => o.Status == "Canceled");

            return View(dashboardData);
        }

        [HttpGet]
        public async Task<IActionResult> GetRevenueData(DateTime startDate, DateTime endDate)
        {
            var orders = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .ToListAsync();

            var revenueByDay = orders
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(r => r.Date)
                .ToList();

            var labels = revenueByDay.Select(r => r.Date.ToString("yyyy-MM-dd")).ToList();
            var revenue = revenueByDay.Select(r => r.Revenue).ToList();

            return Json(new { labels, revenue });
        }

        public class DashboardData
        {
            public int PendingOrders { get; set; }
            public int PaidOrders { get; set; }
            public int CompletedOrders { get; set; }
            public int CanceledOrders { get; set; }
            public int TotalUsers { get; set; }
            public int TotalRevenue { get; set; }
            public int TotalOrders { get; set; }
            public int PreviousDayRevenue { get; set; }
            public int PreviousWeekRevenue { get; set; }
            public int PreviousMonthRevenue { get; set; }
            public decimal PercentageChangePreviousDay { get; set; }
            public decimal PercentageChangePreviousWeek { get; set; }
            public decimal PercentageChangePreviousMonth { get; set; }
            public RevenueData RevenueData { get; set; }
            public List<ProductSale> ProductSales { get; set; }
            public List<BrandSale> TopSellingBrands { get; set; }
        }
        public class RevenueData
        {
            public List<int> WeeklyRevenue { get; set; }
            public List<int> MonthlyRevenue { get; set; }
            public List<int> YearlyRevenue { get; set; }
        }
        public class ProductSale
        {
            public int VariantId { get; set; }
            public string ProductName { get; set; }
            public int TotalQuantity { get; set; }
        }
        public class BrandSale
        {
            public int BrandId { get; set; }
            public string BrandName { get; set; }
            public int QuantitySold { get; set; }
        }
    }
}