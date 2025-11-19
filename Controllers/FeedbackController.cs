using Microsoft.AspNetCore.Mvc;
using ECommerceApp.DTOs.FeedbackDTOs;
using ECommerceApp.Services;
using ECommerceApp.DTOs;
namespace ECommerceApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbackController : ControllerBase
    {
        private readonly FeedbackService _feedbackService;
        public FeedbackController(FeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }
        // Submits feedback for a product.
        [HttpPost("SubmitFeedback")]
        public async Task<ActionResult<ApiResponse<FeedbackResponseDTO>>> SubmitFeedback([FromBody] FeedbackCreateDTO feedbackCreateDTO)
        {
            var response = await _feedbackService.SubmitFeedbackAsync(feedbackCreateDTO);
            if (response.StatusCode != 200)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }
        // Retrieves all feedback for a specific product.
        [HttpGet("GetFeedbackForProduct/{productId}")]
        public async Task<ActionResult<ApiResponse<ProductFeedbackResponseDTO>>> GetFeedbackForProduct(int productId)
        {
            var response = await _feedbackService.GetFeedbackForProductAsync(productId);
            if (response.StatusCode != 200)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }
        // Retrieves all feedback (Admin use).
        [HttpGet("GetAllFeedback")]
        public async Task<ActionResult<ApiResponse<List<FeedbackResponseDTO>>>> GetAllFeedback()
        {
            var response = await _feedbackService.GetAllFeedbackAsync();
            if (response.StatusCode != 200)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }
        // Updates a specific feedback entry.
        [HttpPut("UpdateFeedback")]
        public async Task<ActionResult<ApiResponse<FeedbackResponseDTO>>> UpdateFeedback([FromBody] FeedbackUpdateDTO feedbackUpdateDTO)
        {
            var response = await _feedbackService.UpdateFeedbackAsync(feedbackUpdateDTO);
            if (response.StatusCode != 200)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }
        // Deletes a specific feedback entry.
        [HttpDelete("DeleteFeedback")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponseDTO>>> DeleteFeedback([FromBody] FeedbackDeleteDTO feedbackDeleteDTO)
        {
            var response = await _feedbackService.DeleteFeedbackAsync(feedbackDeleteDTO);
            if (response.StatusCode != 200)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }
    }
}