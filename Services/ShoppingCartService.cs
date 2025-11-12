using Microsoft.EntityFrameworkCore;
using ECommerceApp.Data;
using ECommerceApp.DTOs.ShoppingCartDTOs;
using ECommerceApp.DTOs;
using ECommerceApp.Models;
namespace ECommerceApp.Services
{
    public class ShoppingCartService
    {
        // Dependency injection of the ApplicationDbContext to interact with the database.
        private readonly ApplicationDbContext _context;
        public ShoppingCartService(ApplicationDbContext context)
        {
            _context = context;
        }
        // Retrieves the active (non-checked-out) cart for a given customer.
        // If no active cart exists, an empty cart (with price details set to 0) is returned.
        public async Task<ApiResponse<CartResponseDTO>> GetCartByCustomerIdAsync(int customerId)
        {
            try
            {
                // Query the database for a cart that belongs to the specified customer and is not checked out.
                var cart = await _context.Carts
                .Include(c => c.CartItems) // Include the cart items in the query
                .ThenInclude(ci => ci.Product) // Also include the product details for each cart item
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && !c.IsCheckedOut);
                // If no active cart is found, create an empty DTO with default values.
                if (cart == null)
                {
                    var emptyCartDTO = new CartResponseDTO
                    {
                        CustomerId = customerId,
                        IsCheckedOut = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        CartItems = new List<CartItemResponseDTO>(),
                        TotalBasePrice = 0,
                        TotalDiscount = 0,
                        TotalAmount = 0
                    };
                    // Return the empty cart wrapped in an ApiResponse with status code 200 (OK).
                    return new ApiResponse<CartResponseDTO>(200, emptyCartDTO);
                }
                // Map the cart entity to its corresponding DTO (includes price calculations).
                var cartDTO = MapCartToDTO(cart);
                return new ApiResponse<CartResponseDTO>(200, cartDTO);
            }
            catch (Exception ex)
            {
                // In case of an exception, return a 500 status code with an error message.
                return new ApiResponse<CartResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }
        // Adds a product to the customer's cart.
        // Creates an active cart if one does not exist.
        // If the product already exists in the cart, its quantity is updated.
        public async Task<ApiResponse<CartResponseDTO>> AddToCartAsync(AddToCartDTO addToCartDTO)
        {
            try
            {
                // Retrieve the product from the database using the provided ProductId.
                var product = await _context.Products.FindAsync(addToCartDTO.ProductId);
                if (product == null)
                {
                    // Return 404 if the product is not found.
                    return new ApiResponse<CartResponseDTO>(404, "Product not found.");
                }
                // Check if the requested quantity exceeds the available stock.
                if (addToCartDTO.Quantity > product.StockQuantity)
                {
                    return new ApiResponse<CartResponseDTO>(400, $"Only {product.StockQuantity} units of {product.Name} are available.");
                }
                // Retrieve an active cart for the customer (include related CartItems and Products).
                var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.CustomerId == addToCartDTO.CustomerId && !c.IsCheckedOut);
                // If no active cart exists, create a new cart.
                if (cart == null)
                {
                    cart = new Cart
                    {
                        CustomerId = addToCartDTO.CustomerId,
                        IsCheckedOut = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        CartItems = new List<CartItem>()
                    };
                    // Add the new cart to the database and save changes to generate an Id.
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }
                // Check if the product is already in the cart.
                var existingCartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == addToCartDTO.ProductId);
                if (existingCartItem != null)
                {
                    // If the new total quantity exceeds stock, return an error.
                    if (existingCartItem.Quantity + addToCartDTO.Quantity > product.StockQuantity)
                    {
                        return new ApiResponse<CartResponseDTO>(400, $"Adding {addToCartDTO.Quantity} exceeds available stock.");
                    }
                    // Update the quantity and recalculate the total price for this cart item.
                    existingCartItem.Quantity += addToCartDTO.Quantity;
                    existingCartItem.TotalPrice = (existingCartItem.UnitPrice - existingCartItem.Discount) * existingCartItem.Quantity;
                    existingCartItem.UpdatedAt = DateTime.UtcNow;
                    // Mark the cart item as modified.
                    _context.CartItems.Update(existingCartItem);
                }
                else
                {
                    // Calculate discount per unit, if applicable.
                    var discount = product.DiscountPercentage > 0 ? product.Price * product.DiscountPercentage / 100 : 0;
                    // Create a new CartItem with the product and quantity details.
                    var cartItem = new CartItem
                    {
                        CartId = cart.Id,
                        ProductId = product.Id,
                        Quantity = addToCartDTO.Quantity,
                        UnitPrice = product.Price,
                        Discount = discount,
                        TotalPrice = (product.Price - discount) * addToCartDTO.Quantity,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    // Add the new cart item to the database context.
                    _context.CartItems.Add(cartItem);
                }
                // Update the cart's last updated timestamp.
                cart.UpdatedAt = DateTime.UtcNow;
                _context.Carts.Update(cart);
                await _context.SaveChangesAsync();
                // Reload the cart with the latest details (including related CartItems and Products).
                cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.Id == cart.Id) ?? new Cart();
                // Map the cart entity to the DTO, which includes price calculations.
                var cartDTO = MapCartToDTO(cart);
                return new ApiResponse<CartResponseDTO>(200, cartDTO);
            }
            catch (Exception ex)
            {
                // Return error response in case of exceptions.
                return new ApiResponse<CartResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }
        // Updates the quantity of a specific item in the customer's cart.
        public async Task<ApiResponse<CartResponseDTO>> UpdateCartItemAsync(UpdateCartItemDTO updateCartItemDTO)
        {
            try
            {
                // Retrieve the active cart for the customer along with cart items and product details.
                var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.CustomerId == updateCartItemDTO.CustomerId && !c.IsCheckedOut);
                // Return 404 if no active cart is found.
                if (cart == null)
                {
                    return new ApiResponse<CartResponseDTO>(404, "Active cart not found.");
                }
                // Find the specific cart item that needs to be updated.
                var cartItem = cart.CartItems.FirstOrDefault(ci => ci.Id == updateCartItemDTO.CartItemId);
                if (cartItem == null)
                {
                    return new ApiResponse<CartResponseDTO>(404, "Cart item not found.");
                }
                // Ensure the updated quantity does not exceed the available product stock.
                if (updateCartItemDTO.Quantity > cartItem.Product.StockQuantity)
                {
                    return new ApiResponse<CartResponseDTO>(400, $"Only {cartItem.Product.StockQuantity} units of {cartItem.Product.Name} are available.");
                }
                // Update the cart item's quantity and recalculate its total price.
                cartItem.Quantity = updateCartItemDTO.Quantity;
                cartItem.TotalPrice = (cartItem.UnitPrice - cartItem.Discount) * cartItem.Quantity;
                cartItem.UpdatedAt = DateTime.UtcNow;
                // Mark the cart item as updated.
                _context.CartItems.Update(cartItem);
                // Update the cart's updated timestamp.
                cart.UpdatedAt = DateTime.UtcNow;
                _context.Carts.Update(cart);
                await _context.SaveChangesAsync();
                // Reload the updated cart with its items.
                cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.Id == cart.Id) ?? new Cart();
                // Map the updated cart to the DTO.
                var cartDTO = MapCartToDTO(cart);
                return new ApiResponse<CartResponseDTO>(200, cartDTO);
            }
            catch (Exception ex)
            {
                // Return error response if an exception occurs.
                return new ApiResponse<CartResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }
        // Removes a specific item from the customer's cart.
        public async Task<ApiResponse<CartResponseDTO>> RemoveCartItemAsync(RemoveCartItemDTO removeCartItemDTO)
        {
            try
            {
                // Retrieve the active cart along with its items and product details.
                var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.CustomerId == removeCartItemDTO.CustomerId && !c.IsCheckedOut);
                // Return 404 if no active cart is found.
                if (cart == null)
                {
                    return new ApiResponse<CartResponseDTO>(404, "Active cart not found.");
                }
                // Find the cart item to remove.
                var cartItem = cart.CartItems.FirstOrDefault(ci => ci.Id == removeCartItemDTO.CartItemId);
                if (cartItem == null)
                {
                    return new ApiResponse<CartResponseDTO>(404, "Cart item not found.");
                }
                // Remove the cart item from the context.
                _context.CartItems.Remove(cartItem);
                cart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                // Reload the updated cart after removal.
                cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.Id == cart.Id) ?? new Cart();
                // Map the updated cart to the DTO.
                var cartDTO = MapCartToDTO(cart);
                return new ApiResponse<CartResponseDTO>(200, cartDTO);
            }
            catch (Exception ex)
            {
                // Return error response if an exception occurs.
                return new ApiResponse<CartResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }
        // Clears all items from the customer's active cart.
        public async Task<ApiResponse<ConfirmationResponseDTO>> ClearCartAsync(int customerId)
        {
            try
            {
                // Retrieve the active cart along with its items.
                var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && !c.IsCheckedOut);
                // Return 404 if no active cart is found.
                if (cart == null)
                {
                    return new ApiResponse<ConfirmationResponseDTO>(404, "Active cart not found.");
                }
                // If there are any items in the cart, remove them.
                if (cart.CartItems.Any())
                {
                    _context.CartItems.RemoveRange(cart.CartItems);
                    cart.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
                // Create a confirmation response DTO.
                var confirmation = new ConfirmationResponseDTO
                {
                    Message = "Cart has been cleared successfully."
                };
                return new ApiResponse<ConfirmationResponseDTO>(200, confirmation);
            }
            catch (Exception ex)
            {
                // Return error response if an exception occurs.
                return new ApiResponse<ConfirmationResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }
        // Helper method to manually map a Cart entity to a CartResponseDTO.
        // This method also calculates the TotalBasePrice, TotalDiscount, and TotalAmount.
        private CartResponseDTO MapCartToDTO(Cart cart)
        {
            // Map each CartItem entity to its corresponding CartItemResponseDTO.
            var cartItemsDto = cart.CartItems?.Select(ci => new CartItemResponseDTO
            {
                Id = ci.Id,
                ProductId = ci.ProductId,
                ProductName = ci.Product?.Name,
                Quantity = ci.Quantity,
                UnitPrice = ci.UnitPrice,
                Discount = ci.Discount,
                TotalPrice = ci.TotalPrice
            }).ToList() ?? new List<CartItemResponseDTO>();
            // Initialize totals for base price, discount, and amount after discount.
            decimal totalBasePrice = 0;
            decimal totalDiscount = 0;
            decimal totalAmount = 0;
            // Iterate through each cart item DTO to accumulate the totals.
            foreach (var item in cartItemsDto)
            {
                totalBasePrice += item.UnitPrice * item.Quantity;       // Sum of base prices (without discount)
                totalDiscount += item.Discount * item.Quantity;         // Sum of discounts applied per unit
                totalAmount += item.TotalPrice;                         // Sum of final prices after discount
            }
            // Create and return the final CartResponseDTO with all details and calculated totals.
            return new CartResponseDTO
            {
                Id = cart.Id,
                CustomerId = cart.CustomerId,
                IsCheckedOut = cart.IsCheckedOut,
                CreatedAt = cart.CreatedAt,
                UpdatedAt = cart.UpdatedAt,
                CartItems = cartItemsDto,
                TotalBasePrice = totalBasePrice,
                TotalDiscount = totalDiscount,
                TotalAmount = totalAmount
            };
        }
    }
}