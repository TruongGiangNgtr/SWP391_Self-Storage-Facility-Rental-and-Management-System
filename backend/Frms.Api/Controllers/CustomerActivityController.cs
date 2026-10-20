using Frms.Api.Authorization;
using Frms.Api.DTOs.Responses;
using Frms.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

/// <summary>
/// Customer-only proposals pending SRS change approval. No new persisted state.
/// This is a read-only delivery history, NOT a read/unread notification inbox.
/// </summary>
[Authorize(Roles = RoleNames.Customer)]
[Route("api/v1/customers/me")]
public sealed class CustomerActivityController(ICustomerActivityService service) : ScaffoldControllerBase
{
    [HttpGet("notifications")]
    [ProducesResponseType(typeof(PaginatedResponse<CustomerNotificationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<CustomerNotificationResponse>>> ListNotifications(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await service.NotificationsAsync(page, pageSize, ct);
        return Ok(new PaginatedResponse<CustomerNotificationResponse>(
            result.Items.Select(x => new CustomerNotificationResponse(
                x.NotificationLogId, x.NotificationType, x.Content,
                Utc(x.CreatedAt), Utc(x.SentAt))).ToArray(),
            Page(page, pageSize, result.TotalItems)));
    }

    [HttpGet("transactions")]
    [ProducesResponseType(typeof(PaginatedResponse<CustomerPaymentHistoryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<CustomerPaymentHistoryResponse>>> ListTransactions(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await service.PaymentsAsync(page, pageSize, ct);
        return Ok(new PaginatedResponse<CustomerPaymentHistoryResponse>(
            result.Items.Select(x => new CustomerPaymentHistoryResponse(
                x.PaymentId, x.InvoiceId, x.InvoiceType, x.Amount, x.PaymentMethod, x.Status,
                x.PaidAt.HasValue ? Utc(x.PaidAt.Value) : null, Utc(x.CreatedAt))).ToArray(),
            Page(page, pageSize, result.TotalItems)));
    }

    private static PaginationResponse Page(int page, int pageSize, long total)
    {
        if (total > int.MaxValue)
            throw new InvalidOperationException("Customer activity exceeds supported pagination capacity.");
        var pages = total == 0 ? 0 : (int)((total - 1) / pageSize + 1);
        return new PaginationResponse(page, pageSize, (int)total, pages);
    }
    private static DateTimeOffset Utc(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
