using Microsoft.AspNetCore.Mvc;
using ECommerceApp.DTOs;
using ECommerceApp.DTOs.ShoppingCartDTOs;
using ECommerceApp.Services;
namespace ECommerceApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartsController : ControllerBase
    {
        private readonly ShoppingCartService _shoppingCartService;
        public CartsController(ShoppingCartService shoppingCartService)
        {
            _shoppingCartService = shoppingCartService;
        }
        // Retrieves the active cart for a given customer.
        [HttpGet("GetCart/{customerId}")]
        public async Task<ActionResult<ApiResponse<CartResponseDTO>>> GetCartByCustomerId(int customerId)
        {
            var response = await _shoppingCartService.GetCartByCustomerIdAsync(customerId);
            if (response.StatusCode != 200)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }
        // Adds an item to the customer's cart.
        [HttpPost("AddToCart")]
        public async Task<ActionResult<ApiResponse<CartResponseDTO>>> AddToCart([FromBody] AddToCartDTO addToCartDTO)
        {
            var response = await _shoppingCartService.AddToCartAsync(addToCartDTO);
            if (response.StatusCode != 200)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }
        // Updates the quantity of an existing cart item.
        [HttpPut("UpdateCartItem")]
        public async Task<ActionResult<ApiResponse<CartResponseDTO>>> UpdateCartItem([FromBody] UpdateCartItemDTO updateCartItemDTO)
        {
            var response = await _shoppingCartService.UpdateCartItemAsync(updateCartItemDTO);
            if (response.StatusCode != 200)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }
        // Removes a specific item from the cart.
        [HttpDelete("RemoveCartItem")]
        public async Task<ActionResult<ApiResponse<CartResponseDTO>>> RemoveCartItem([FromBody] RemoveCartItemDTO removeCartItemDTO)
        {
            var response = await _shoppingCartService.RemoveCartItemAsync(removeCartItemDTO);
            if (response.StatusCode != 200)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }
        // Clears all items from the customer's active cart.
        [HttpDelete("ClearCart")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponseDTO>>> ClearCart([FromQuery] int customerId)
        {
            var response = await _shoppingCartService.ClearCartAsync(customerId);
            if (response.StatusCode != 200)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }
    }
}