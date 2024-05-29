using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using App.Data;
using App.Models;
using App.Areas.Identity.Models.ManageViewModels;

namespace XEDAPVIP.Areas.Admin.Controllers
{
    [Authorize(Roles = RoleName.Administrator)]
    [Area("Admin")]
    [Route("Admin/Order")]
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Order
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            ViewBag.orderCount = _context.Orders.Count();

            return View(await _context.Orders.ToListAsync());
        }

        // GET: Admin/Order/Details/5
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Where(o => o.Id == id)
                .Include(o => o.OrderDetails) // Include order details
                .ThenInclude(od => od.Variant)
                .ThenInclude(v => v.Product)
                .Select(o => new OrderViewModel
                {
                    OrderId = o.Id,
                    UserName = o.UserName,
                    UserEmail = o.UserEmail,
                    PhoneNumber = o.PhoneNumber,
                    OrderNote = o.OrderNote,
                    OrderDate = o.OrderDate,
                    ShippedDate = o.ShippedDate,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    ShippingAddress = o.ShippingAddress,
                    ShippingMethod = o.ShippingMethod,
                    PaymentMethod = o.PaymentMethod,
                    OrderDetails = o.OrderDetails.Select(d => new OrderDetailViewModel
                    {
                        ProductName = d.Variant.Product.Name,
                        ProductDescription = d.ProductDescription,
                        ProductImage = d.ProductImage,
                        Quantity = d.Quantity,
                        VariantId = d.VariantId,
                        Variant = d.Variant,
                        OrderId = d.OrderId,
                        UnitPrice = d.UnitPrice,
                        TotalPrice = d.Quantity * d.UnitPrice
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Admin/Order/Create
        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Order/Edit
        [HttpPost("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string status)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Variant)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            // Valid status transitions
            var validTransitions = new Dictionary<string, List<string>>()
            {
                { "Pending", new List<string> { "Paid", "Canceled" } },
                { "Paid", new List<string> { "Delivering" } },
                { "Delivering", new List<string> { "Completed", "Failed" } },
                { "Completed", new List<string>() },
                { "Canceled", new List<string>() },
                { "Failed", new List<string>() }
            };

            // Check if the status transition is valid
            if (!validTransitions[order.Status].Contains(status))
            {
                return BadRequest("Invalid status transition.");
            }

            // Update status and stock if needed
            if (status == "Canceled" || status == "Failed")
            {
                foreach (var detail in order.OrderDetails)
                {
                    var variant = detail.Variant;
                    if (variant != null)
                    {
                        variant.Quantity += detail.Quantity; // Restore the quantity
                        _context.Update(variant);
                    }
                }
            }
            if (status == "Delivering")
            {
                order.ShippedDate = DateTime.Now;
            }

            order.Status = status;
            _context.Update(order);

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã thay đổi trạng thái đơn hàng thành công";

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Order/Delete
        [HttpPost("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Xoá thành công đơn hàn {id}";

            return Ok(new { success = true });
        }
    }
}
