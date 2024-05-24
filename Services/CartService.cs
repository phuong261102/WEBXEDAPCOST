using Newtonsoft.Json;
using XEDAPVIP.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using App.Models;

public class CartService
{
    public const string CARTKEY = "CART";
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly HttpContext _httpContext;

    public CartService(AppDbContext context, IHttpContextAccessor contextAccessor)
    {
        _context = context;
        _contextAccessor = contextAccessor;
        _httpContext = contextAccessor.HttpContext;

    }

    public List<CartItem> GetCartItems(string userId = null)
    {
        if (!string.IsNullOrEmpty(userId))
        {

            return _context.CartItems
                .Where(ci => ci.UserId == userId)
                .Include(ci => ci.Variant)
                .Include(ci => ci.Variant.Product)
                .ToList();
        }
        else
        {
            var session = _httpContext.Session;
            string jsonCart = session.GetString(CARTKEY);
            if (jsonCart != null)
            {
                return JsonConvert.DeserializeObject<List<CartItem>>(jsonCart);
            }
            return new List<CartItem>();
        }
    }

    public void ClearCart(string userId = null)
    {
        if (!string.IsNullOrEmpty(userId))
        {
            var cartItems = _context.CartItems.Where(ci => ci.UserId == userId).ToList();
            _context.CartItems.RemoveRange(cartItems);
            _context.SaveChanges();
        }
        else
        {
            var session = _httpContext.Session;
            session.Remove(CARTKEY);
        }
    }

    public void SaveCartItems(string userId, List<CartItem> cartItems)
    {
        if (!string.IsNullOrEmpty(userId))
        {
            // Lấy danh sách cart items hiện tại của user từ cơ sở dữ liệu
            var existingCartItems = _context.CartItems.Where(ci => ci.UserId == userId).ToList();

            // Duyệt qua từng cart item được cung cấp để kiểm tra và cập nhật hoặc thêm mới
            foreach (var item in cartItems)
            {
                var existingItem = existingCartItems.FirstOrDefault(ci => ci.VariantId == item.VariantId);

                if (existingItem != null)
                {
                    // Nếu item với VariantId tương ứng tồn tại, cập nhật thông tin 
                    existingItem.Quantity = item.Quantity;
                    // Thêm bất kỳ cập nhật thuộc tính nào khác cần thiết từ item lên existingItem
                }
                else
                {
                    // Nếu không tìm thấy item với VariantId trong cơ sở dữ liệu, thêm nó như một mục mới
                    item.UserId = userId;
                    _context.CartItems.Add(item);
                }
            }

            // Xóa các cart items cũ không còn trong danh sách mới được cung cấp
            foreach (var existingItem in existingCartItems)
            {
                if (!cartItems.Any(ci => ci.VariantId == existingItem.VariantId))
                {
                    _context.CartItems.Remove(existingItem);
                }
            }

            // Lưu các thay đổi vào cơ sở dữ liệu
            _context.SaveChanges();
        }
        else
        {
            // Nếu không có userId, lưu cart vào session
            var session = _httpContext.Session;
            string jsonCart = JsonConvert.SerializeObject(cartItems);
            session.SetString(CARTKEY, jsonCart);
        }
    }
}

