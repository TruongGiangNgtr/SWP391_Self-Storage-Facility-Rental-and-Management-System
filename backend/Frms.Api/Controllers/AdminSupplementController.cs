using Frms.Api.Authorization;
using Frms.Api.DTOs.Responses;
using Frms.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Authorize(Roles = RoleNames.SystemAdministrator)]
[Route("api/v1/admin")]
public sealed class AdminSupplementController(IAdminReadService service, IAdminCustomerStatusService customerStatuses) : ScaffoldControllerBase
{
    [HttpPost("customers/{customerId:guid}/activate")]
    public async Task<ActionResult<ApiResponse<AdminCustomerStatusResponse>>> ActivateCustomer(
        Guid customerId, CancellationToken cancellationToken)
    {
        var result = await customerStatuses.SetStatusAsync(customerId, "ACTIVE", cancellationToken);
        return Ok(new ApiResponse<AdminCustomerStatusResponse>(new(
            result.CustomerId, result.UserAccountId, result.Status)));
    }

    [HttpPost("customers/{customerId:guid}/deactivate")]
    public async Task<ActionResult<ApiResponse<AdminCustomerStatusResponse>>> DeactivateCustomer(
        Guid customerId, CancellationToken cancellationToken)
    {
        var result = await customerStatuses.SetStatusAsync(customerId, "INACTIVE", cancellationToken);
        return Ok(new ApiResponse<AdminCustomerStatusResponse>(new(
            result.CustomerId, result.UserAccountId, result.Status)));
    }

    [HttpGet("login-history")]
    public async Task<ActionResult<PaginatedResponse<AdminLoginHistoryResponse>>> ListLoginHistory(
        [FromQuery] Guid? userAccountId = null, [FromQuery] string? status = null,
        [FromQuery] DateTime? fromUtc = null, [FromQuery] DateTime? toUtc = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await service.LoginHistoryAsync(userAccountId, status,
            fromUtc, toUtc, page, pageSize, cancellationToken);
        return Ok(new PaginatedResponse<AdminLoginHistoryResponse>(
            items.Select(x => new AdminLoginHistoryResponse(x.LoginHistoryId, x.UserAccountId,
                x.LoginAt, x.IpAddress, x.DeviceInfo, x.Status)).ToArray(),
            new PaginationResponse(page, pageSize, total,
                total == 0 ? 0 : (int)Math.Ceiling((double)total / pageSize))));
    }

    [HttpGet("audit-logs")]
    public async Task<ActionResult<PaginatedResponse<AdminAuditLogResponse>>> ListAuditLogs(
        [FromQuery] Guid? userAccountId = null, [FromQuery] string? entityType = null,
        [FromQuery] Guid? entityId = null, [FromQuery] string? action = null,
        [FromQuery] DateTime? fromUtc = null, [FromQuery] DateTime? toUtc = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await service.AuditLogsAsync(userAccountId, entityType,
            entityId, action, fromUtc, toUtc, page, pageSize, cancellationToken);
        return Ok(new PaginatedResponse<AdminAuditLogResponse>(
            items.Select(x => new AdminAuditLogResponse(x.AuditLogId, x.UserAccountId,
                x.Action, x.EntityType, x.EntityId, x.OldValue, x.NewValue, x.CreatedAt)).ToArray(),
            new PaginationResponse(page, pageSize, total,
                total == 0 ? 0 : (int)Math.Ceiling((double)total / pageSize))));
    }

    // PROPOSED UX-RBAC-01/11: no operational/financial data exposure.
    [HttpGet("facilities/lookup")]
    public async Task<ActionResult<PaginatedResponse<AdminFacilityLookupResponse>>> FacilityLookup(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await service.FacilityLookupAsync(page, pageSize, cancellationToken);
        return Ok(new PaginatedResponse<AdminFacilityLookupResponse>(
            items.Select(x => new AdminFacilityLookupResponse(x.FacilityId, x.Name, x.Status)).ToArray(),
            new PaginationResponse(page, pageSize, total,
                total == 0 ? 0 : (int)Math.Ceiling((double)total / pageSize))));
    }

    [HttpGet("customers/{customerId:guid}/deactivation-eligibility")]
    public ActionResult<ApiErrorResponse> CustomerDeactivationEligibility(Guid customerId) =>
        ScaffoldNotImplemented("UX-RBAC-11");
}
