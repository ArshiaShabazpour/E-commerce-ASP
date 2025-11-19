using ECommerceApp.DTOs;
using ECommerceApp.DTOs.RefundDTOs;
using ECommerceApp.Services;
using Microsoft.AspNetCore.Mvc;
namespace ECommerceValidationDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RefundsController : ControllerBase
    {
        private readonly RefundService _refundService;
        public RefundsController(RefundService refundService)
        {
            _refundService = refundService;
        }
        // GET: api/Refunds/GetEligibleRefunds
        // Returns approved cancellations that have no associated refund entry.
        [HttpGet("GetEligibleRefunds")]
        public async Task<ActionResult<ApiResponse<List<PendingRefundResponseDTO>>>> GetEligibleRefunds()
        {
            var response = await _refundService.GetEligibleRefundsAsync();
            if (response.StatusCode != 200)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }
        // POST: api/Refunds/ProcessRefund
        // Initiates a refund for approved cancellations without an existing refund record.
        [HttpPost("ProcessRefund")]
        public async Task<ActionResult<ApiResponse<RefundResponseDTO>>> ProcessRefund([FromBody] RefundRequestDTO refundRequest)
        {
            var response = await _refundService.ProcessRefundAsync(refundRequest);
            if (response.StatusCode != 200)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }
        // PUT: api/Refunds/UpdateRefundStatus
        // Manually reprocesses a refund (only applicable if the refund is pending or failed).
        [HttpPut("UpdateRefundStatus")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponseDTO>>> UpdateRefundStatus([FromBody] RefundStatusUpdateDTO statusUpdate)
        {
            var response = await _refundService.UpdateRefundStatusAsync(statusUpdate);
            if (response.StatusCode != 200)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }
        // GET: api/Refunds/GetRefundById/{id}
        // Retrieves a refund by its ID.
        [HttpGet("GetRefundById/{id}")]
        public async Task<ActionResult<ApiResponse<RefundResponseDTO>>> GetRefundById(int id)
        {
            var response = await _refundService.GetRefundByIdAsync(id);
            if (response.StatusCode != 200)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }
        // GET: api/Refunds/GetAllRefunds
        // Retrieves all refunds.
        [HttpGet("GetAllRefunds")]
        public async Task<ActionResult<ApiResponse<List<RefundResponseDTO>>>> GetAllRefunds()
        {
            var response = await _refundService.GetAllRefundsAsync();
            if (response.StatusCode != 200)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }
    }
}