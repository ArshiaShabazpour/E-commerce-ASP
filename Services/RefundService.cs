using Microsoft.EntityFrameworkCore;
using ECommerceApp.Data;
using ECommerceApp.Models;
using ECommerceApp.DTOs.RefundDTOs;
using ECommerceApp.DTOs;
using System.Globalization;

namespace ECommerceApp.Services
{
    public class RefundService
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly TemplateService _templateService;

        public RefundService(ApplicationDbContext context, EmailService emailService, TemplateService templateService)
        {
            _context = context;
            _emailService = emailService;
            _templateService = templateService;
        }

        // Retrieves all eligible cancellations for refund initiation.
        public async Task<ApiResponse<List<PendingRefundResponseDTO>>> GetEligibleRefundsAsync()
        {
            var eligible = await _context.Cancellations
                .Include(c => c.Order)
                .ThenInclude(o => o.Payment)
                .Where(c => c.Status == CancellationStatus.Approved && c.Refund == null
                            && c.Order.Payment.PaymentMethod.ToLower() != "cod")
                .Select(c => new PendingRefundResponseDTO
                {
                    CancellationId = c.Id,
                    OrderId = c.OrderId,
                    OrderAmount = c.OrderAmount,
                    CancellationCharge = c.CancellationCharges ?? 0.00m,
                    ComputedRefundAmount = c.OrderAmount - (c.CancellationCharges ?? 0.00m),
                    CancellationRemarks = c.Remarks
                }).ToListAsync();
            return new ApiResponse<List<PendingRefundResponseDTO>>(200, eligible);
        }

        // Processes refund request
        public async Task<ApiResponse<RefundResponseDTO>> ProcessRefundAsync(RefundRequestDTO refundRequest)
        {
            var cancellation = await _context.Cancellations
                .Include(c => c.Order)
                .ThenInclude(o => o.Payment)
                .Include(c => c.Order)
                .ThenInclude(o => o.Customer)
                .FirstOrDefaultAsync(c => c.Id == refundRequest.CancellationId);

            if (cancellation == null)
                return new ApiResponse<RefundResponseDTO>(404, "Cancellation request not found.");

            if (cancellation.Status != CancellationStatus.Approved)
                return new ApiResponse<RefundResponseDTO>(400, "Only approved cancellations are eligible for refunds.");

            var existingRefund = await _context.Refunds
                .FirstOrDefaultAsync(r => r.CancellationId == refundRequest.CancellationId);

            if (existingRefund != null)
                return new ApiResponse<RefundResponseDTO>(400, "Refund for this cancellation request has already been initiated.");

            var payment = cancellation.Order.Payment;
            if (payment == null || payment.PaymentMethod.ToLower() == "cod")
                return new ApiResponse<RefundResponseDTO>(400, "No payment associated with the order.");

            decimal computedRefundAmount = cancellation.OrderAmount - (cancellation.CancellationCharges ?? 0.00m);
            if (computedRefundAmount <= 0)
                return new ApiResponse<RefundResponseDTO>(400, "Computed refund amount is not valid.");

            var refund = new Refund
            {
                CancellationId = refundRequest.CancellationId,
                PaymentId = payment.Id,
                Amount = computedRefundAmount,
                RefundMethod = refundRequest.RefundMethod.ToString(),
                RefundReason = refundRequest.RefundReason,
                Status = RefundStatus.Pending,
                InitiatedAt = DateTime.UtcNow,
                ProcessedBy = refundRequest.ProcessedBy
            };

            _context.Refunds.Add(refund);
            await _context.SaveChangesAsync();

            var gatewayResponse = await ProcessRefundPaymentAsync(refund);

            if (gatewayResponse.IsSuccess)
            {
                refund.Status = RefundStatus.Completed;
                refund.TransactionId = gatewayResponse.TransactionId;
                refund.CompletedAt = DateTime.UtcNow;
                payment.Status = PaymentStatus.Refunded;
                _context.Payments.Update(payment);

                if (cancellation.Order.Customer != null && !string.IsNullOrEmpty(cancellation.Order.Customer.Email))
                {
                    var emailBody = await GenerateRefundSuccessEmailBodyAsync(refund, cancellation.Order.OrderNumber, cancellation);
                    await _emailService.SendEmailAsync(
                        cancellation.Order.Customer.Email,
                        $"Your Refund Has Been Processed Successfully, Order #{cancellation.Order.OrderNumber}",
                        emailBody,
                        IsBodyHtml: true);
                }
            }
            else
            {
                refund.Status = RefundStatus.Failed;
            }

            _context.Refunds.Update(refund);
            await _context.SaveChangesAsync();

            return new ApiResponse<RefundResponseDTO>(200, MapRefundToDTO(refund));
        }

        // Admin can manually update refund status
        public async Task<ApiResponse<ConfirmationResponseDTO>> UpdateRefundStatusAsync(RefundStatusUpdateDTO statusUpdate)
        {
            var refund = await _context.Refunds
                .Include(r => r.Cancellation)
                .ThenInclude(c => c.Order)
                .ThenInclude(o => o.Customer)
                .Include(r => r.Payment)
                .FirstOrDefaultAsync(r => r.Id == statusUpdate.RefundId);

            if (refund == null)
                return new ApiResponse<ConfirmationResponseDTO>(404, "Refund not found.");

            if (refund.Status != RefundStatus.Pending && refund.Status != RefundStatus.Failed)
                return new ApiResponse<ConfirmationResponseDTO>(400, "Only pending or failed refunds can be updated.");

            refund.RefundMethod = statusUpdate.RefundMethod.ToString();
            refund.Status = RefundStatus.Completed;
            refund.TransactionId = statusUpdate.TransactionId;
            refund.CompletedAt = DateTime.UtcNow;
            refund.ProcessedBy = statusUpdate.ProcessedBy;
            refund.RefundReason = statusUpdate.RefundReason;

            refund.Payment.Status = PaymentStatus.Refunded;
            _context.Payments.Update(refund.Payment);
            _context.Refunds.Update(refund);
            await _context.SaveChangesAsync();

            if (refund.Cancellation?.Order?.Customer != null && !string.IsNullOrEmpty(refund.Cancellation.Order.Customer.Email))
            {
                var emailBody = await GenerateRefundSuccessEmailBodyAsync(refund, refund.Cancellation.Order.OrderNumber, refund.Cancellation);
                await _emailService.SendEmailAsync(
                    refund.Cancellation.Order.Customer.Email,
                    $"Your Refund Has Been Processed Successfully, Order #{refund.Cancellation.Order.OrderNumber}",
                    emailBody,
                    IsBodyHtml: true);
            }

            return new ApiResponse<ConfirmationResponseDTO>(200, new ConfirmationResponseDTO
            {
                Message = $"Refund with ID {refund.Id} has been updated to {refund.Status}."
            });
        }

        // Get refund by ID
        public async Task<ApiResponse<RefundResponseDTO>> GetRefundByIdAsync(int id)
        {
            var refund = await _context.Refunds
                .Include(r => r.Cancellation)
                .ThenInclude(c => c.Order)
                .ThenInclude(o => o.Payment)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (refund == null)
                return new ApiResponse<RefundResponseDTO>(404, "Refund not found.");

            return new ApiResponse<RefundResponseDTO>(200, MapRefundToDTO(refund));
        }

        // Get all refunds
        public async Task<ApiResponse<List<RefundResponseDTO>>> GetAllRefundsAsync()
        {
            var refunds = await _context.Refunds
                .Include(r => r.Cancellation)
                .ThenInclude(c => c.Order)
                .ThenInclude(o => o.Payment)
                .ToListAsync();

            var refundList = refunds.Select(r => MapRefundToDTO(r)).ToList();
            return new ApiResponse<List<RefundResponseDTO>>(200, refundList);
        }

        // Private helper: map Refund to DTO
        private RefundResponseDTO MapRefundToDTO(Refund refund)
        {
            return new RefundResponseDTO
            {
                Id = refund.Id,
                CancellationId = refund.CancellationId,
                PaymentId = refund.PaymentId,
                Amount = refund.Amount,
                RefundMethod = Enum.Parse<RefundMethod>(refund.RefundMethod),
                RefundReason = refund.RefundReason,
                Status = refund.Status,
                InitiatedAt = refund.InitiatedAt,
                CompletedAt = refund.CompletedAt,
                TransactionId = refund.TransactionId
            };
        }

        // Simulates payment gateway processing
        public async Task<PaymentGatewayRefundResponseDTO> ProcessRefundPaymentAsync(Refund refund)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            var random = new Random();
            double chance = random.NextDouble();

            if (chance < 0.70)
                return new PaymentGatewayRefundResponseDTO
                {
                    IsSuccess = true,
                    Status = RefundStatus.Completed,
                    TransactionId = $"TXN-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}"
                };
            if (chance < 0.90)
                return new PaymentGatewayRefundResponseDTO
                {
                    IsSuccess = false,
                    Status = RefundStatus.Failed
                };

            return new PaymentGatewayRefundResponseDTO
            {
                IsSuccess = false,
                Status = RefundStatus.Pending
            };
        }

        // Generates HTML email body using TemplateService
        public async Task<string> GenerateRefundSuccessEmailBodyAsync(Refund refund, string orderNumber, Cancellation cancellation)
        {
            TimeZoneInfo userTimeZone = GetCustomerTimeZone(cancellation.Order.Customer) ?? TimeZoneInfo.Utc;

            string completedAtStr = refund.CompletedAt.HasValue
                ? TimeZoneInfo.ConvertTimeFromUtc(refund.CompletedAt.Value, userTimeZone).ToString("dd MMM yyyy HH:mm:ss")
                : "N/A";

            var values = new Dictionary<string, string>
            {
                { "OrderNumber", orderNumber },
                { "RefundTransactionId", refund.TransactionId ?? "N/A" },
                { "OrderAmount", cancellation.OrderAmount.ToString("0.00") },
                { "CancellationCharges", (cancellation.CancellationCharges ?? 0.00m).ToString("0.00") },
                { "CancellationReason", cancellation.Reason },
                { "RefundMethod", refund.RefundMethod },
                { "RefundAmount", refund.Amount.ToString("0.00") },
                { "CompletedAt", completedAtStr }
            };

            string template = await _templateService.LoadTemplateAsync("RefundSuccessTemplate.html");
            return _templateService.ReplacePlaceholders(template, values);
        }

        // Fallback timezone for customer
        private TimeZoneInfo GetCustomerTimeZone(Customer? customer)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("UTC"); // replace with actual user preference if needed
        }
    }
}
