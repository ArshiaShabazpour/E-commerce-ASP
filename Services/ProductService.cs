using Microsoft.EntityFrameworkCore;
using ECommerceApp.Data;
using ECommerceApp.DTOs.ProductDTOs;
using ECommerceApp.Models;
using ECommerceApp.DTOs;
namespace ECommerceApp.Services
{
    public class ProductService
    {
        private readonly ApplicationDbContext _context;
        public ProductService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<ApiResponse<ProductResponseDTO>> CreateProductAsync(ProductCreateDTO productDto)
        {
            try
            {
                // Check if product name already exists (case-insensitive)
                if (await _context.Products.AnyAsync(p => p.Name.ToLower() == productDto.Name.ToLower()))
                {
                    return new ApiResponse<ProductResponseDTO>(400, "Product name already exists.");
                }
                // Check if Category exists
                if (!await _context.Categories.AnyAsync(cat => cat.Id == productDto.CategoryId))
                {
                    return new ApiResponse<ProductResponseDTO>(400, "Specified category does not exist.");
                }
                // Manual mapping from DTO to Model
                var product = new Product
                {
                    Name = productDto.Name,
                    Description = productDto.Description,
                    Price = productDto.Price,
                    StockQuantity = productDto.StockQuantity,
                    ImageUrl = productDto.ImageUrl,
                    DiscountPercentage = productDto.DiscountPercentage,
                    CategoryId = productDto.CategoryId,
                    IsAvailable = true
                };
                // Add product to the database
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                // Map to ProductResponseDTO
                var productResponse = new ProductResponseDTO
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    StockQuantity = product.StockQuantity,
                    ImageUrl = product.ImageUrl,
                    DiscountPercentage = product.DiscountPercentage,
                    CategoryId = product.CategoryId,
                    IsAvailable = product.IsAvailable
                };
                return new ApiResponse<ProductResponseDTO>(200, productResponse);
            }
            catch (Exception ex)
            {
                // Log the exception (implementation depends on your logging setup)
                return new ApiResponse<ProductResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }
        public async Task<ApiResponse<ProductResponseDTO>> GetProductByIdAsync(int id)
        {
            try
            {
                var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
                if (product == null)
                {
                    return new ApiResponse<ProductResponseDTO>(404, "Product not found.");
                }
                // Map to ProductResponseDTO
                var productResponse = new ProductResponseDTO
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    StockQuantity = product.StockQuantity,
                    ImageUrl = product.ImageUrl,
                    DiscountPercentage = product.DiscountPercentage,
                    CategoryId = product.CategoryId,
                    IsAvailable = product.IsAvailable
                };
                return new ApiResponse<ProductResponseDTO>(200, productResponse);
            }
            catch (Exception ex)
            {
                // Log the exception
                return new ApiResponse<ProductResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }
        public async Task<ApiResponse<ConfirmationResponseDTO>> UpdateProductAsync(ProductUpdateDTO productDto)
        {
            try
            {
                var product = await _context.Products.FindAsync(productDto.Id);
                if (product == null)
                {
                    return new ApiResponse<ConfirmationResponseDTO>(404, "Product not found.");
                }
                // Check if the new product name already exists (case-insensitive), excluding the current product
                if (await _context.Products.AnyAsync(p => p.Name.ToLower() == productDto.Name.ToLower() && p.Id != productDto.Id))
                {
                    return new ApiResponse<ConfirmationResponseDTO>(400, "Another product with the same name already exists.");
                }
                // Check if Category exists
                if (!await _context.Categories.AnyAsync(cat => cat.Id == productDto.CategoryId))
                {
                    return new ApiResponse<ConfirmationResponseDTO>(400, "Specified category does not exist.");
                }
                // Update product properties manually
                product.Name = productDto.Name;
                product.Description = productDto.Description;
                product.Price = productDto.Price;
                product.StockQuantity = productDto.StockQuantity;
                product.ImageUrl = productDto.ImageUrl;
                product.DiscountPercentage = productDto.DiscountPercentage;
                product.CategoryId = productDto.CategoryId;
                await _context.SaveChangesAsync();
                // Prepare confirmation message
                var confirmationMessage = new ConfirmationResponseDTO
                {
                    Message = $"Product with Id {productDto.Id} updated successfully."
                };
                return new ApiResponse<ConfirmationResponseDTO>(200, confirmationMessage);
            }
            catch (Exception ex)
            {
                // Log the exception
                return new ApiResponse<ConfirmationResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }
        public async Task<ApiResponse<ConfirmationResponseDTO>> DeleteProductAsync(int id)
        {
            try
            {
                var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id);
                if (product == null)
                {
                    return new ApiResponse<ConfirmationResponseDTO>(404, "Product not found.");
                }
                // Implementing Soft Delete
                product.IsAvailable = false;
                await _context.SaveChangesAsync();
                // Prepare confirmation message
                var confirmationMessage = new ConfirmationResponseDTO
                {
                    Message = $"Product with Id {id} deleted successfully."
                };
                return new ApiResponse<ConfirmationResponseDTO>(200, confirmationMessage);
            }
            catch (Exception ex)
            {
                // Log the exception
                return new ApiResponse<ConfirmationResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }
        public async Task<ApiResponse<List<ProductResponseDTO>>> GetAllProductsAsync()
        {
            try
            {
                var products = await _context.Products
                .AsNoTracking()
                .ToListAsync();
                var productList = products.Select(p => new ProductResponseDTO
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                    ImageUrl = p.ImageUrl,
                    DiscountPercentage = p.DiscountPercentage,
                    CategoryId = p.CategoryId,
                    IsAvailable = p.IsAvailable
                }).ToList();
                return new ApiResponse<List<ProductResponseDTO>>(200, productList);
            }
            catch (Exception ex)
            {
                // Log the exception
                return new ApiResponse<List<ProductResponseDTO>>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }
        public async Task<ApiResponse<List<ProductResponseDTO>>> GetAllProductsByCategoryAsync(int categoryId)
        {
            try
            {
                // Retrieve products associated with the specified category
                var products = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.CategoryId == categoryId && p.IsAvailable)
                .ToListAsync();
                if (products == null || products.Count == 0)
                {
                    return new ApiResponse<List<ProductResponseDTO>>(404, "Products not found.");
                }
                var productList = products.Select(p => new ProductResponseDTO
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                    ImageUrl = p.ImageUrl,
                    DiscountPercentage = p.DiscountPercentage,
                    CategoryId = p.CategoryId,
                    IsAvailable = p.IsAvailable
                }).ToList();
                return new ApiResponse<List<ProductResponseDTO>>(200, productList);
            }
            catch (Exception ex)
            {
                // Log the exception
                return new ApiResponse<List<ProductResponseDTO>>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }
        public async Task<ApiResponse<ConfirmationResponseDTO>> UpdateProductStatusAsync(ProductStatusUpdateDTO productStatusUpdateDTO)
        {
            try
            {
                var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productStatusUpdateDTO.ProductId);
                if (product == null)
                {
                    return new ApiResponse<ConfirmationResponseDTO>(404, "Product not found.");
                }
                product.IsAvailable = productStatusUpdateDTO.IsAvailable;
                await _context.SaveChangesAsync();
                // Prepare confirmation message
                var confirmationMessage = new ConfirmationResponseDTO
                {
                    Message = $"Product with Id {productStatusUpdateDTO.ProductId} Status Updated successfully."
                };
                return new ApiResponse<ConfirmationResponseDTO>(200, confirmationMessage);
            }
            catch (Exception ex)
            {
                // Log the exception
                return new ApiResponse<ConfirmationResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }
    }
} 