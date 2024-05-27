using App.Models;
using Microsoft.EntityFrameworkCore;
using XEDAPVIP.Models;

public class OrderService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly HttpContext _httpContext;
    private readonly CartService _cartService;

    public OrderService(AppDbContext context, IHttpContextAccessor contextAccessor, CartService cartService)
    {
        _context = context;
        _contextAccessor = contextAccessor;
        _httpContext = contextAccessor.HttpContext;
        _cartService = cartService;
    }

    // Create a new order
    public async Task<Order> CreateOrderAsync(string? userId, string userName, string phoneNumber, string userEmail, string? orderNote, List<int> cartItemIds, string shippingAddress, string shippingMethod, string paymentMethod, string totalAmount, string status)
    {
        List<CartItem> cartItems;

        if (string.IsNullOrEmpty(userId))
        {
            // Guest user - retrieve cart items from session
            cartItems = _cartService.GetCartItems();
            userId = "Guest";
        }
        else
        {
            // Logged-in user - retrieve cart items from database
            cartItems = await _context.CartItems
                .Where(ci => cartItemIds.Contains(ci.Id))
                .Include(ci => ci.Variant)
                    .ThenInclude(v => v.Product)
                .ToListAsync();
        }

        if (!cartItems.Any())
        {
            throw new InvalidOperationException("Cart items not found.");
        }

        var order = new Order
        {
            UserId = userId,
            UserName = userName,
            PhoneNumber = phoneNumber,
            UserEmail = userEmail,
            OrderNote = orderNote,
            OrderDate = DateTime.Now,
            Status = status,
            ShippingAddress = shippingAddress,
            ShippingMethod = shippingMethod,
            PaymentMethod = paymentMethod,
            TotalAmount = totalAmount
        };

        // Save the order to get the OrderId
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Create order details
        var orderDetails = cartItems.Select(ci => new OrderDetail
        {
            OrderId = order.Id, // Set the OrderId
            VariantId = ci.Variant.Id,
            ProductName = ci.Variant.Product.Name,
            ProductDescription = $"Màu sắc: {ci.Variant.Color}, Kích thước: {ci.Variant.Size}",
            ProductImage = ci.Variant.Product.MainImage,
            Quantity = ci.Quantity,
            UnitPrice = ci.Variant.Product.DiscountPrice ?? ci.Variant.Product.Price
        }).ToList();
        foreach (var orderItem in orderDetails)
        {
            var productVariant = await _context.productVariants
                                                    .Include(pv => pv.Product)
                                                    .FirstOrDefaultAsync(pv => pv.Id == orderItem.VariantId);
        }
        _context.OrderDetails.AddRange(orderDetails);
        await _context.SaveChangesAsync();
        await SaveOrderImage(order.OrderDetails);
        await UpdateProductQuantities(cartItems);

        // Remove Cart Items and Update Product Quantities
        if (string.IsNullOrEmpty(userId))
        {
            // Clear session cart items for guest user
            _cartService.ClearCart();
        }
        else
        {
            // Clear database cart items for logged-in user
            await RemoveCartItems(cartItems.Select(ci => ci.Id).ToList());
        }


        return order;
    }
    private async Task SaveOrderImage(List<OrderDetail> orderDetails)
    {
        foreach (var orderDetail in orderDetails)
        {
            try
            {
                var productSlug = orderDetail.Variant.Product.Slug;
                var mainImageFileName = orderDetail.Variant.Product.MainImage; // Assuming MainImage is the file name or path as a string

                // Define the source and destination directory paths
                var sourceDirectory = Path.Combine(Directory.GetCurrentDirectory(), $"wwwroot/images/products/{productSlug}");
                var destinationDirectory = Path.Combine(Directory.GetCurrentDirectory(), $"wwwroot/images/order/{productSlug}-{orderDetail.Variant.Id}-{orderDetail.OrderId}");

                // Ensure the source directory exists
                if (!Directory.Exists(sourceDirectory))
                {
                    throw new DirectoryNotFoundException($"Source directory not found: {sourceDirectory}");
                }

                // Ensure the destination directory exists
                Directory.CreateDirectory(destinationDirectory);

                // Define the file paths
                var sourceFilePath = Path.Combine(sourceDirectory, mainImageFileName);
                var destinationFilePath = Path.Combine(destinationDirectory, mainImageFileName);

                // Ensure the source file exists
                if (!File.Exists(sourceFilePath))
                {
                    throw new FileNotFoundException($"Source file not found: {sourceFilePath}");
                }

                // Copy the file to the new location
                using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
                using (var destinationStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write))
                {
                    await sourceStream.CopyToAsync(destinationStream);
                }

                // Update the path in the database or in-memory object if necessary
                orderDetail.ProductImage = $"/images/order/{productSlug}order/{mainImageFileName}";
            }
            catch (UnauthorizedAccessException ex)
            {
                // Log the exception using a logging framework (e.g., Serilog, NLog, etc.)
                // Example: _logger.LogError(ex, "Access to the path is denied.");
                throw new InvalidOperationException("Access to the path is denied.", ex);
            }
            catch (Exception ex)
            {
                // Log the exception
                // Example: _logger.LogError(ex, "Error saving image for product {ProductSlug}", orderDetail.Variant.Product.Slug);
                throw new InvalidOperationException($"An error occurred while saving the image for product {orderDetail.Variant.Product.Slug}.", ex);
            }
        }
    }



    private async Task RemoveCartItems(List<int> cartItemIds)
    {
        var cartItems = await _context.CartItems.Where(ci => cartItemIds.Contains(ci.Id)).ToListAsync();
        _context.CartItems.RemoveRange(cartItems);
        await _context.SaveChangesAsync();
    }

    private async Task UpdateProductQuantities(List<CartItem> cartItems)
    {
        foreach (var item in cartItems)
        {
            item.Variant.Quantity -= item.Quantity;
            if (item.Variant.Quantity < 0)
            {
                item.Variant.Quantity = 0; // Ensure stock quantity doesn't go negative
            }
        }
        await _context.SaveChangesAsync();
    }

    // Get orders for a user
    public async Task<List<Order>> GetOrdersAsync(string? userId)
    {
        return await _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Variant)
            .ThenInclude(v => v.Product)
            .ToListAsync();
    }

    // Get a specific order by ID
    public async Task<Order> GetOrderByIdAsync(int orderId)
    {
        return await _context.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Variant)
            .ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    // Delete an order
    public async Task<bool> DeleteOrderAsync(int orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
        {
            return false;
        }

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();

        return true;
    }
}
