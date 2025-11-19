using Microsoft.EntityFrameworkCore;
using ECommerceApp.Data;
using ECommerceApp.DTOs.FeedbackDTOs;
using ECommerceApp.Models;
using ECommerceApp.DTOs;
namespace ECommerceApp.Services
{
    public class FeedbackService
    {
        private readonly ApplicationDbContext _context;
        public FeedbackService(ApplicationDbContext context)
        {
            _context = context;
        }
        // Submits new feedback for a product.
        public async Task<ApiResponse<FeedbackResponseDTO>> SubmitFeedbackAsync(FeedbackCreateDTO feedbackCreateDTO)
        {
            try
            {
                // Verify customer exists
                var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == feedbackCreateDTO.CustomerId);
                if (customer == null)
                {
                    return new ApiResponse<FeedbackResponseDTO>(404, "Customer not found.");
                }
                // Verify product exists
                var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == feedbackCreateDTO.ProductId);
                if (product == null)
                {
                    return new ApiResponse<FeedbackResponseDTO>(404, "Product not found.");
                }
                // Verify order item exists and belongs to customer and product (Order must be delivered)
                var orderItem = await _context.OrderItems
                .Include(oi => oi.Order)
                .AsNoTracking()
                .FirstOrDefaultAsync(oi =>
                oi.ProductId == feedbackCreateDTO.ProductId &&
                oi.Order.CustomerId == feedbackCreateDTO.CustomerId &&
                oi.Order.OrderStatus == OrderStatus.Delivered);
                if (orderItem == null)
                {
                    return new ApiResponse<FeedbackResponseDTO>(400, "Invalid OrderItemId. Customer must have purchased the product.");
                }
                // Check if feedback already exists for this order item
                if (await _context.Feedbacks.AnyAsync(fed => fed.CustomerId == feedbackCreateDTO.CustomerId && fed.ProductId == feedbackCreateDTO.ProductId))
                {
                    return new ApiResponse<FeedbackResponseDTO>(400, "Feedback for this product and order item already exists.");
                }
                // Create new feedback entity
                var feedback = new Feedback
                {
                    CustomerId = feedbackCreateDTO.CustomerId,
                    ProductId = feedbackCreateDTO.ProductId,
                    Rating = feedbackCreateDTO.Rating,
                    Comment = feedbackCreateDTO.Comment,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Feedbacks.Add(feedback);
                await _context.SaveChangesAsync();
                // Prepare response DTO (manual mapping)
                var feedbackResponse = new FeedbackResponseDTO
                {
                    Id = feedback.Id,
                    CustomerId = customer.Id,
                    CustomerName = $"{customer.FirstName} {customer.LastName}",
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Rating = feedback.Rating,
                    Comment = feedback.Comment,
                    CreatedAt = feedback.CreatedAt,
                    UpdatedAt = feedback.UpdatedAt
                };
                return new ApiResponse<FeedbackResponseDTO>(200, feedbackResponse);
            }
            catch (Exception ex)
            {
                // Log the exception (implementation depends on your logging setup)
                return new ApiResponse<FeedbackResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }
        // Retrieves all feedback for a specific product along with the average rating.
        public async Task<ApiResponse<ProductFeedbackResponseDTO>> GetFeedbackForProductAsync(int productId)
        {
            try
            {
                // Verify product exists
                var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId);
                if (product == null)
                {
                    return new ApiResponse<ProductFeedbackResponseDTO>(404, "Product not found.");
                }
                // Retrieve feedbacks for the specified product, including customer details, with no tracking for performance
                var feedbacks = await _context.Feedbacks
                .Where(f => f.ProductId == productId)
                .Include(f => f.Customer)
                .AsNoTracking()
                .ToListAsync();
                double averageRating = 0;
                List<CustomerFeedback> customerFeedbacks = new List<CustomerFeedback>();
                if (feedbacks.Any())
                {
                    averageRating = feedbacks.Average(f => f.Rating);
                    customerFeedbacks = feedbacks.Select(f => new CustomerFeedback
                    {
                        Id = f.Id,
                        CustomerId = f.CustomerId,
                        CustomerName = $"{f.Customer.FirstName} {f.Customer.LastName}",
                        Rating = f.Rating,
                        Comment = f.Comment,
                        CreatedAt = f.CreatedAt,
                        UpdatedAt = f.UpdatedAt
                    }).ToList();
                }
                var productFeedbackResponse = new ProductFeedbackResponseDTO
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    AverageRating = Math.Round(averageRating, 2),
                    Feedbacks = customerFeedbacks
                };
                return new ApiResponse<ProductFeedbackResponseDTO>(200, productFeedbackResponse);
            }
            catch (Exception ex)
            {
                // Log the exception (implementation depends on your logging setup)
                return new ApiResponse<ProductFeedbackResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }
        // Retrieves all feedback entries in the system.
        public async Task<ApiResponse<List<FeedbackResponseDTO>>> GetAllFeedbackAsync()
        {
            try
            {
                var feedbacks = await _context.Feedbacks
                .Include(f => f.Customer)
                .Include(f => f.Product)
                .AsNoTracking()
                .ToListAsync();
                var feedbackResponseList = feedbacks.Select(f => new FeedbackResponseDTO
                {
                    Id = f.Id,
                    CustomerId = f.CustomerId,
                    CustomerName = $"{f.Customer.FirstName} {f.Customer.LastName}",
                    ProductId = f.ProductId,
                    ProductName = f.Product.Name,
                    Rating = f.Rating,
                    Comment = f.Comment,
                    CreatedAt = f.CreatedAt,
                    UpdatedAt = f.UpdatedAt
                }).ToList();
                return new ApiResponse<List<FeedbackResponseDTO>>(200, feedbackResponseList);
            }
            catch (Exception ex)
            {
                // Log the exception (implementation depends on your logging setup)
                return new ApiResponse<List<FeedbackResponseDTO>>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }
        // Updates an existing feedback entry.
        public async Task<ApiResponse<FeedbackResponseDTO>> UpdateFeedbackAsync(FeedbackUpdateDTO feedbackUpdateDTO)
        {
            try
            {
                // Retrieve the feedback along with its customer and product information
                var feedback = await _context.Feedbacks
                .Include(f => f.Customer)
                .Include(f => f.Product)
                .FirstOrDefaultAsync(f => f.Id == feedbackUpdateDTO.FeedbackId
                && f.CustomerId == feedbackUpdateDTO.CustomerId);
                if (feedback == null)
                {
                    return new ApiResponse<FeedbackResponseDTO>(404, "Either Feedback or Customer not found.");
                }
                // Update the feedback details
                feedback.Rating = feedbackUpdateDTO.Rating;
                feedback.Comment = feedbackUpdateDTO.Comment;
                feedback.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                var feedbackResponse = new FeedbackResponseDTO
                {
                    Id = feedback.Id,
                    CustomerId = feedback.CustomerId,
                    CustomerName = $"{feedback.Customer.FirstName} {feedback.Customer.LastName}",
                    ProductId = feedback.ProductId,
                    ProductName = feedback.Product.Name,
                    Rating = feedback.Rating,
                    Comment = feedback.Comment,
                    CreatedAt = feedback.CreatedAt,
                    UpdatedAt = feedback.UpdatedAt
                };
                return new ApiResponse<FeedbackResponseDTO>(200, feedbackResponse);
            }
            catch (Exception ex)
            {
                // Log the exception (implementation depends on your logging setup)
                return new ApiResponse<FeedbackResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }
        // Deletes a feedback entry.
        public async Task<ApiResponse<ConfirmationResponseDTO>> DeleteFeedbackAsync(FeedbackDeleteDTO feedbackDeleteDTO)
        {
            try
            {
                var feedback = await _context.Feedbacks.FindAsync(feedbackDeleteDTO.FeedbackId);
                if (feedback == null)
                {
                    return new ApiResponse<ConfirmationResponseDTO>(404, "Feedback not found.");
                }
                // Ensure that only the owner can delete the feedback
                if (feedback.CustomerId != feedbackDeleteDTO.CustomerId)
                {
                    return new ApiResponse<ConfirmationResponseDTO>(401, "You are not authorized to delete this feedback.");
                }
                _context.Feedbacks.Remove(feedback);
                await _context.SaveChangesAsync();
                var confirmation = new ConfirmationResponseDTO
                {
                    Message = $"Feedback with Id {feedbackDeleteDTO.FeedbackId} deleted successfully."
                };
                return new ApiResponse<ConfirmationResponseDTO>(200, confirmation);
            }
            catch (Exception ex)
            {
                // Log the exception (implementation depends on your logging setup)
                return new ApiResponse<ConfirmationResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }
    }
}