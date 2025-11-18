using Microsoft.EntityFrameworkCore;
using ECommerceApp.Data;
using ECommerceApp.DTOs.CancellationDTOs;
using ECommerceApp.Models;
using ECommerceApp.DTOs;
namespace ECommerceApp.Services
{
    public class CancellationService
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly TemplateService _templateService;

        public CancellationService(ApplicationDbContext context, EmailService emailService, TemplateService templateService)
        {
            _context = context;
            _emailService = emailService;
            _templateService = templateService;
        }
        // Handles a cancellation request from a customer.
        public async Task<ApiResponse<CancellationResponseDTO>> RequestCancellationAsync(CancellationRequestDTO cancellationRequest)
        {
            try
            {
                // Validate order existence with its items and product details (read-only)
                var order = await _context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == cancellationRequest.OrderId &&
                o.CustomerId == cancellationRequest.CustomerId);
                if (order == null)
                {
                    return new ApiResponse<CancellationResponseDTO>(404, "Order not found.");
                }
                // Check if order is eligible for cancellation (only Processing)
                if (order.OrderStatus != OrderStatus.Processing)
                {
                    return new ApiResponse<CancellationResponseDTO>(400, "Order is not eligible for cancellation.");
                }
                // Check if a cancellation request for the order already exists
                var existingCancellation = await _context.Cancellations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.OrderId == cancellationRequest.OrderId);
                if (existingCancellation != null)
                {
                    return new ApiResponse<CancellationResponseDTO>(400, "A cancellation request for this order already exists.");
                }
                // Create the new cancellation record
                var cancellation = new Cancellation
                {
                    OrderId = cancellationRequest.OrderId,
                    Reason = cancellationRequest.Reason,
                    Status = CancellationStatus.Pending,
                    RequestedAt = DateTime.UtcNow,
                    OrderAmount = order.TotalAmount,
                    CancellationCharges = 0.00m, // default zero; admin may update later if needed.
                };
                _context.Cancellations.Add(cancellation);
                await _context.SaveChangesAsync();
                // Mapping from Cancellation to CancellationResponseDTO
                var cancellationResponse = new CancellationResponseDTO
                {
                    Id = cancellation.Id,
                    OrderId = cancellation.OrderId,
                    Reason = cancellation.Reason,
                    OrderAmount = order.TotalAmount,
                    Status = cancellation.Status,
                    RequestedAt = cancellation.RequestedAt,
                    CancellationCharges = cancellation.CancellationCharges
                };
                return new ApiResponse<CancellationResponseDTO>(200, cancellationResponse);
            }
            catch (Exception ex)
            {
                // Log exception as needed
                return new ApiResponse<CancellationResponseDTO>(500, $"An unexpected error occurred: {ex.Message}");
            }
        }
        // Retrieves a cancellation request by its ID.
        public async Task<ApiResponse<CancellationResponseDTO>> GetCancellationByIdAsync(int id)
        {
            try
            {
                var cancellation = await _context.Cancellations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
                if (cancellation == null)
                {
                    return new ApiResponse<CancellationResponseDTO>(404, "Cancellation request not found.");
                }
                var cancellationResponse = new CancellationResponseDTO
                {
                    Id = cancellation.Id,
                    OrderId = cancellation.OrderId,
                    Reason = cancellation.Reason, //Provided by Client
                    Status = cancellation.Status,
                    RequestedAt = cancellation.RequestedAt,
                    ProcessedAt = cancellation.ProcessedAt,
                    ProcessedBy = cancellation.ProcessedBy,
                    Remarks = cancellation.Remarks, //Provided by Admin
                    OrderAmount = cancellation.OrderAmount,
                    CancellationCharges = cancellation.CancellationCharges
                };
                return new ApiResponse<CancellationResponseDTO>(200, cancellationResponse);
            }
            catch (Exception ex)
            {
                return new ApiResponse<CancellationResponseDTO>(500, $"An unexpected error occurred: {ex.Message}");
            }
        }
        // Updates the status of a cancellation request (approval/rejection) by an administrator.
        // Also handles order status update and stock restoration if approved.
        public async Task<ApiResponse<ConfirmationResponseDTO>> UpdateCancellationStatusAsync(CancellationStatusUpdateDTO statusUpdate)
        {
            // Begin a transaction to ensure atomic operations
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var cancellation = await _context.Cancellations
                    .Include(c => c.Order)
                    .ThenInclude(cust => cust.Customer)
                    .FirstOrDefaultAsync(c => c.Id == statusUpdate.CancellationId);
                    if (cancellation == null)
                    {
                        return new ApiResponse<ConfirmationResponseDTO>(404, "Cancellation request not found.");
                    }
                    if (cancellation.Status != CancellationStatus.Pending)
                    {
                        return new ApiResponse<ConfirmationResponseDTO>(400, "Only pending cancellation requests can be updated.");
                    }
                    // Update the cancellation status and metadata
                    cancellation.Status = statusUpdate.Status;
                    cancellation.ProcessedAt = DateTime.UtcNow;
                    cancellation.ProcessedBy = statusUpdate.ProcessedBy;
                    cancellation.Remarks = statusUpdate.Remarks;
                    if (statusUpdate.Status == CancellationStatus.Approved)
                    {
                        // Update the order status to Canceled
                        cancellation.Order.OrderStatus = OrderStatus.Canceled;
                        cancellation.CancellationCharges = statusUpdate.CancellationCharges;
                        // Restore stock quantities for each order item
                        var orderItems = await _context.OrderItems
                        .Include(oi => oi.Product)
                        .Where(oi => oi.OrderId == cancellation.OrderId)
                        .ToListAsync();
                        foreach (var item in orderItems)
                        {
                            item.Product.StockQuantity += item.Quantity;
                            _context.Products.Update(item.Product);
                        }
                    }
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    // Optionally, notify the customer and admin about the status update
                    // Integrate your notification/email service as needed.
                    if (statusUpdate.Status == CancellationStatus.Approved)
                    {
                        await NotifyCancellationAcceptedAsync(cancellation);
                    }
                    else if (statusUpdate.Status == CancellationStatus.Rejected)
                    {
                        await NotifyCancellationRejectionAsync(cancellation);
                    }
                    var confirmation = new ConfirmationResponseDTO
                    {
                        Message = $"Cancellation request with ID {cancellation.Id} has been {cancellation.Status}."
                    };
                    return new ApiResponse<ConfirmationResponseDTO>(200, confirmation);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return new ApiResponse<ConfirmationResponseDTO>(500, $"An unexpected error occurred: {ex.Message}");
                }
            }
        }
        // Retrieves all cancellation requests used by Admin.
        public async Task<ApiResponse<List<CancellationResponseDTO>>> GetAllCancellationsAsync()
        {
            try
            {
                var cancellations = await _context.Cancellations
                .AsNoTracking()
                .Include(c => c.Order)
                .ToListAsync();
                var cancellationList = cancellations.Select(c => new CancellationResponseDTO
                {
                    Id = c.Id,
                    OrderId = c.OrderId,
                    Reason = c.Reason,
                    Status = c.Status,
                    RequestedAt = c.RequestedAt,
                    ProcessedAt = c.ProcessedAt,
                    ProcessedBy = c.ProcessedBy,
                    OrderAmount = c.OrderAmount,
                    CancellationCharges = c.CancellationCharges,
                    Remarks = c.Remarks,
                }).ToList();
                return new ApiResponse<List<CancellationResponseDTO>>(200, cancellationList);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<CancellationResponseDTO>>(500, $"An unexpected error occurred: {ex.Message}");
            }
        }
        // Notify customers about cancellation status changes.
        private async Task NotifyCancellationAcceptedAsync(Cancellation cancellation)
        {
            if (cancellation.Order == null || cancellation.Order.Customer == null)
            {
                return;
            }

            string subject = $"Cancellation Request Update - Order #{cancellation.Order.OrderNumber}";

            string template = await _templateService.LoadTemplateAsync("CancellationAccepted.html");

            var values = new Dictionary<string, string>
            {   
                    { "FirstName", cancellation.Order.Customer.FirstName },
                    { "LastName", cancellation.Order.Customer.LastName },
                    { "OrderNumber", cancellation.Order.OrderNumber.ToString() },
                    { "CancellationReason", cancellation.Reason },
                    { "AdminRemark", cancellation.Remarks ?? "N/A" },
                    { "RequestedAt", cancellation.RequestedAt.ToString("MMMM dd, yyyy HH:mm") },
                    { "ProcessedAt", cancellation.ProcessedAt.HasValue ? cancellation.ProcessedAt.Value.ToString("MMMM dd, yyyy HH:mm") : "N/A" },
                    { "OrderAmount", cancellation.OrderAmount.ToString() },
                    { "CancellationCharges", cancellation.CancellationCharges?.ToString() ?? "0" },
                    { "RefundableAmount", (cancellation.OrderAmount - (cancellation.CancellationCharges ?? 0)).ToString() },
                    { "Status", cancellation.Status.ToString() }
            };

            string emailBody = _templateService.ReplacePlaceholders(template, values);

            await _emailService.SendEmailAsync(cancellation.Order.Customer.Email, subject, emailBody, IsBodyHtml: true);
        }

        private async Task NotifyCancellationRejectionAsync(Cancellation cancellation)
        {
            if (cancellation.Order == null || cancellation.Order.Customer == null)
            {
                return;
            }

            string subject = $"Cancellation Request Rejected - Order #{cancellation.Order.OrderNumber}";

            string template = await _templateService.LoadTemplateAsync("CancellationRejected.html");

            var values = new Dictionary<string, string>
            {
                { "FirstName", cancellation.Order.Customer.FirstName },
                { "LastName", cancellation.Order.Customer.LastName },
                { "OrderNumber", cancellation.Order.OrderNumber.ToString() },
                { "CancellationReason", cancellation.Reason },
                { "AdminRemark", cancellation.Remarks ?? "N/A" },
                { "RequestedAt", cancellation.RequestedAt.ToString("MMMM dd, yyyy HH:mm") },
                { "Status", cancellation.Status.ToString() }
            };

            string emailBody = _templateService.ReplacePlaceholders(template, values);

            await _emailService.SendEmailAsync(cancellation.Order.Customer.Email, subject, emailBody, IsBodyHtml: true);
        }
    }
}