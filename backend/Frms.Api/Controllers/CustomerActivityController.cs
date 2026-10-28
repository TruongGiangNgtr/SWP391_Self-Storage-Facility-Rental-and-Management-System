using Frms.Api.Authorization;
using Frms.Api.DTOs.Responses;
using Frms.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

/// <summary>
/// Own Customer business notifications (OD-06) and Payment history (OD-07).
/// Notifications are visible after commit independently of SMTP; no read/unread state.
/// </summary>
[Authorize(Roles = RoleNames.Customer)]
[Route("api/v1/customers/me")]
public sealed class CustomerActivityController(ICustomerActivityService service) : ControllerBase
{
    /// <summary>OD-06: Read own committed in-app business notices, newest first.</summary>
    /// <remarks>SMTP, Status and SentAt do not determine visibility. Legacy/email-only rows are excluded.</remarks>
    [HttpGet("notifications")]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(PaginatedResponse<CustomerNotificationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<CustomerNotificationResponse>>> ListNotifications(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await service.NotificationsAsync(page, pageSize, ct);
        return Ok(new PaginatedResponse<CustomerNotificationResponse>(
            result.Items.Select(x => new CustomerNotificationResponse(
                x.NotificationLogId, x.NotificationType, x.Content,
                Utc(x.CreatedAt))).ToArray(),
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
