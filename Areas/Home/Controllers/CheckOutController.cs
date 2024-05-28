using System.Diagnostics;
using App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using XEDAPVIP.Areas.Home.Models.CheckOut;
using XEDAPVIP.Models;
using XEDAPVIP.Services;
using static XEDAPVIP.Areas.Home.Models.CheckOut.ProfileCheckoutModel;
namespace App.Areas.Home.Controllers
{
    [Area("Home")]
    public class CheckOutController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProductViewController> _logger;
        private readonly IVnPayService _vnPayService;
        private readonly UserManager<AppUser> _userManager;
        private readonly CacheService _cacheService;
        private readonly CartService _cartService;
        private readonly HttpClient _httpClient;
        private readonly OrderService _orderService;
        public CheckOutController(AppDbContext context, ILogger<ProductViewController> logger, UserManager<AppUser> userManager, CacheService cacheService,
        CartService cartService, OrderService orderService, IVnPayService vnPayService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _cacheService = cacheService;
            _cartService = cartService;
            _orderService = orderService;
            _vnPayService = vnPayService;
            _httpClient = new HttpClient();
        }
        [HttpPost("CreatePayment")]
        public IActionResult CreatePayment([FromBody] Order order)
        {
            var paymentUrl = _vnPayService.CreatePaymentUrl(order, HttpContext);
            return Ok(new { url = paymentUrl });
        }

        [HttpGet("VnPayReturn")]
        public IActionResult VnPayReturn()
        {
            if (_vnPayService.ValidateResponse(Request.Query))
            {
                return Ok("Payment successful");
            }
            else
            {
                return BadRequest("Payment failed");
            }
        }
        [Route("/Checkout", Name = "Checkout")]
        public async Task<IActionResult> Check_out()
        {
            try
            {
                var categories = await _cacheService.GetCategoriesAsync();
                var brands = await _cacheService.GetBrandsAsync();

                ViewBag.categories = categories;
                ViewBag.brands = brands;

                var user = await GetCurrentUserAsync();
                var userId = user?.Id;

                var provinces = await GetProvincesAsync();
                var provinceOptions = provinces.Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name
                }).ToList();

                List<CartItem> cartItems = !string.IsNullOrEmpty(userId)
                    ? _cartService.GetCartItems(userId)
                    : _cartService.GetCartItems();

                var model = new ProfileCheckoutModel
                {
                    ProvinceOptions = provinceOptions,
                    DistrictOptions = new List<SelectListItem>(),
                    WardOptions = new List<SelectListItem>(),
                    cartItems = cartItems
                };

                if (!string.IsNullOrEmpty(userId))
                {
                    model.UserId = user.Id;
                    model.UserName = user.UserName;
                    model.UserEmail = user.Email;
                    model.PhoneNumber = user.PhoneNumber;
                    model.HomeAddress = user.HomeAddress;
                }

                var antiforgery = HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>();
                var tokenSet = antiforgery.GetAndStoreTokens(HttpContext);
                ViewBag.AntiForgeryToken = tokenSet.RequestToken;

                return View(model);
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex, "Error in Check_out method");
                // Optionally, you can return an error view or a specific error message
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        [HttpPost]
        [Route("/PlaceOrder", Name = "PlaceOrder")]
        public async Task<IActionResult> PlaceOrder([FromBody] OrderRequestModel orderRequest)
        {
            if (orderRequest == null || !orderRequest.CartItemIds.Any())
            {
                _logger.LogWarning("Invalid order request received.");
                return BadRequest("Invalid order request.");
            }

            try
            {
                _logger.LogInformation("Placing order for user {UserId} with items {CartItemIds}",
                    orderRequest.UserId ?? "guest", string.Join(", ", orderRequest.CartItemIds));

                // Retrieve cart items
                List<CartItem> cartItems;
                if (string.IsNullOrEmpty(orderRequest.UserId))
                {
                    // Guest user - retrieve cart items from session
                    cartItems = _cartService.GetCartItems();
                }
                else
                {
                    // Logged-in user - retrieve cart items from database
                    cartItems = _cartService.GetCartItems(orderRequest.UserId);
                }

                if (cartItems == null || !cartItems.Any())
                {
                    _logger.LogWarning("No cart items found for user {UserId}.", orderRequest.UserId ?? "guest");
                    return BadRequest("No cart items found.");
                }

                // Ensure cartItemIds are valid
                var validCartItemIds = cartItems.Select(ci => ci.Id).Intersect(orderRequest.CartItemIds).ToList();
                if (!validCartItemIds.Any())
                {
                    _logger.LogWarning("No valid cart item IDs found for user {UserId}.", orderRequest.UserId ?? "guest");
                    return BadRequest("No valid cart item IDs found.");
                }

                var order = new Order
                {
                    UserId = orderRequest.UserId,
                    UserName = orderRequest.FullName,
                    PhoneNumber = orderRequest.PhoneNumber,
                    UserEmail = orderRequest.EmailAddress,
                    OrderNote = orderRequest.OrderNote,
                    OrderDate = DateTime.Now,
                    Status = orderRequest.Status,
                    ShippingAddress = orderRequest.ShippingAddress,
                    ShippingMethod = orderRequest.ShippingMethod,
                    PaymentMethod = orderRequest.PaymentMethod,
                    TotalAmount = orderRequest.TotalAmount
                };

                if (orderRequest.PaymentMethod == "VNPAY")
                {
                    // Create VNPay payment URL
                    var paymentUrl = _vnPayService.CreatePaymentUrl(order, HttpContext);

                    return Ok(new { url = paymentUrl });

                }

                var createdOrder = await _orderService.CreateOrderAsync(
                    orderRequest.UserId,
                    orderRequest.FullName,
                    orderRequest.PhoneNumber,
                    orderRequest.EmailAddress,
                    orderRequest.OrderNote,
                    validCartItemIds,
                    orderRequest.ShippingAddress,
                    orderRequest.ShippingMethod,
                    orderRequest.PaymentMethod,
                    orderRequest.TotalAmount,
                    orderRequest.Status
                );

                if (order.UserId == null)
                {
                    TempData["SuccessMessage"] = "Đặt hàng thành công. Vui lòng kiểm tra Email để xem chi tiết đơn hàng";
                }
                else
                {
                    TempData["SuccessMessage"] = "Đặt hàng thành công. Vui lòng kiểm tra Email hoặc vào Thông tin cá nhân để xem chi tiết đơn hàng";
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while placing the order for user {UserId}", orderRequest.UserId ?? "guest");
                return StatusCode(500, "An error occurred while placing the order.");
            }
        }


        public bool PaymentCallback()
        {
            var query = HttpContext.Request.Query;

            if (_vnPayService.ValidateResponse(query))
            {
                _logger.LogInformation("VNPAY response is valid.");
                // Handle successful validation, e.g., update order status
                return true;
            }
            else
            {
                _logger.LogWarning("VNPAY response is invalid.");
                // Handle failed validation
                return false;
            }
        }


        [HttpGet]

        public async Task<IActionResult> GetProvinceDistrictWard()
        {
            try
            {
                var apiUrl = "https://raw.githubusercontent.com/kenzouno1/DiaGioiHanhChinhVN/master/data.json";
                HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var apiData = await response.Content.ReadAsStringAsync();
                    return Ok(apiData);
                }
                else
                {
                    // Handle other status codes
                    return StatusCode((int)response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return StatusCode(500);
            }
        }
        public async Task<List<Province>> GetProvincesAsync()
        {
            var dataDiagioiResponse = await GetProvinceDistrictWard();

            if (dataDiagioiResponse is OkObjectResult okResult && okResult.Value != null)
            {
                var apiData = okResult.Value.ToString();
                var dataProvinces = JsonConvert.DeserializeObject<List<Province>>(apiData);
                return dataProvinces;
            }
            else
            {
                // Handle the case where the response is not successful
                return new List<Province>();
            }
        }

        public async Task<JsonResult> GetDistrictsByProvinceId(string provinceId)
        {
            var provinces = await GetProvincesAsync();

            var selectedProvince = provinces.FirstOrDefault(province => province.Id == provinceId);

            if (selectedProvince != null)
            {
                return Json(selectedProvince.Districts);
            }
            else
            {
                return Json(new List<District>());
            }
        }

        public async Task<JsonResult> GetWardsByDistrictId(string districtId)
        {
            var provinces = await GetProvincesAsync();

            District selectedDistrict = null;
            foreach (var province in provinces)
            {
                selectedDistrict = province.Districts.FirstOrDefault(district => district.Id == districtId);
                if (selectedDistrict != null)
                {
                    break;
                }
            }

            if (selectedDistrict != null)
            {
                return Json(selectedDistrict.Wards);
            }
            else
            {
                return Json(new List<Ward>());
            }
        }
        private async Task<string> GetProvinceNameById(string provinceId)
        {
            var provinces = await GetProvincesAsync();
            var province = provinces.FirstOrDefault(p => p.Id == provinceId);
            return province != null ? province.Name : "";
        }

        private async Task<string> GetDistrictNameById(string districtId)
        {
            var provinces = await GetProvincesAsync();
            foreach (var province in provinces)
            {
                var district = province.Districts.FirstOrDefault(d => d.Id == districtId);
                if (district != null)
                {
                    return district.Name;
                }
            }
            return "";
        }

        private async Task<string> GetWardNameById(string wardId)
        {
            var provinces = await GetProvincesAsync();
            foreach (var province in provinces)
            {
                foreach (var district in province.Districts)
                {
                    var ward = district.Wards.FirstOrDefault(w => w.Id == wardId);
                    if (ward != null)
                    {
                        return ward.Name;
                    }
                }
            }
            return "";
        }
        private Task<AppUser> GetCurrentUserAsync()
        {
            return _userManager.GetUserAsync(HttpContext.User);
        }

    }
}