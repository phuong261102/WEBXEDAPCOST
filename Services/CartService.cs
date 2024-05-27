using App.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using XEDAPVIP.Models;

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
                .ThenInclude(v => v.Product)
                .ToList();
        }
        else
        {
            var session = _httpContext.Session;
            string jsonCart = session.GetString(CARTKEY);
            if (jsonCart != null)
            {
                var cartItems = JsonConvert.DeserializeObject<List<CartItem>>(jsonCart);
                return cartItems;
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
            // Get existing cart items for the user
            var existingCartItems = _context.CartItems.Where(ci => ci.UserId == userId).ToList();

            // Iterate over each cart item to either update the existing one or add a new one
            foreach (var item in cartItems)
            {
                var existingItem = existingCartItems.FirstOrDefault(ci => ci.VariantId == item.VariantId);
                if (existingItem != null)
                {
                    // Update quantity if item already exists
                    existingItem.Quantity = item.Quantity;
                }
                else
                {
                    // Add new item
                    item.UserId = userId;
                    _context.CartItems.Add(item);
                }
            }

            // Remove items that are no longer in the cart
            foreach (var existingItem in existingCartItems)
            {
                if (!cartItems.Any(ci => ci.VariantId == existingItem.VariantId))
                {
                    _context.CartItems.Remove(existingItem);
                }
            }

            _context.SaveChanges();
        }
        else
        {
            var session = _httpContext.Session;
            string jsonCart = JsonConvert.SerializeObject(cartItems);
            session.SetString(CARTKEY, jsonCart);
        }
    }
}
