using Frms.Api.Authorization;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Authorize(Roles = RoleNames.SystemAdministrator)]
[Route("api/v1/admin")]
public sealed class AdminController : ScaffoldControllerBase
{
    /// <summary>ADM-001: List user accounts scaffold.</summary>
    [HttpGet("users")]
    public ActionResult<ApiErrorResponse> ListUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) => ScaffoldNotImplemented("ADM-001");

    /// <summary>ADM-002: User/account profile detail scaffold.</summary>
    [HttpGet("users/{userAccountId:guid}")]
    public ActionResult<ApiErrorResponse> GetUser(
        Guid userAccountId,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("ADM-002");

    /// <summary>ADM-003: Activate Customer account scaffold.</summary>
    [HttpPost("customers/{customerId:guid}/activate")]
    public ActionResult<ApiErrorResponse> ActivateCustomer(
        Guid customerId,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("ADM-003");

    /// <summary>ADM-004: Deactivate Customer with lifecycle guard scaffold.</summary>
    [HttpPost("customers/{customerId:guid}/deactivate")]
    public ActionResult<ApiErrorResponse> DeactivateCustomer(
        Guid customerId,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("ADM-004");

    /// <summary>ADM-005: Create Employee account/profile scaffold.</summary>
    [HttpPost("employees")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<ApiErrorResponse> CreateEmployee(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("ADM-005");

    /// <summary>ADM-006: Update Employee basic profile scaffold; exact request schema is pending.</summary>
    [HttpPatch("employees/{employeeId:guid}")]
    public ActionResult<ApiErrorResponse> UpdateEmployee(
        Guid employeeId,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("ADM-006");

    /// <summary>ADM-007: Activate Employee scaffold.</summary>
    [HttpPost("employees/{employeeId:guid}/activate")]
    public ActionResult<ApiErrorResponse> ActivateEmployee(
        Guid employeeId,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("ADM-007");

    /// <summary>ADM-008: Deactivate Employee scaffold.</summary>
    [HttpPost("employees/{employeeId:guid}/deactivate")]
    public ActionResult<ApiErrorResponse> DeactivateEmployee(
        Guid employeeId,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("ADM-008");

    /// <summary>ADM-009: Assign Employee role and Facility scaffold.</summary>
    [HttpPut("employees/{employeeId:guid}/assignment")]
    public ActionResult<ApiErrorResponse> AssignEmployee(
        Guid employeeId,
        [FromBody] AssignEmployeeRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("ADM-009");

    /// <summary>ADM-010: Search LoginHistory scaffold.</summary>
    [HttpGet("login-history")]
    public ActionResult<ApiErrorResponse> SearchLoginHistory(
        [FromQuery] Guid? userAccountId,
        [FromQuery] string? status,
        [FromQuery] DateTimeOffset? fromUtc,
        [FromQuery] DateTimeOffset? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) => ScaffoldNotImplemented("ADM-010");

    /// <summary>ADM-011: Search AuditLog scaffold.</summary>
    [HttpGet("audit-logs")]
    public ActionResult<ApiErrorResponse> SearchAuditLogs(
        [FromQuery] Guid? userAccountId,
        [FromQuery] string? entityType,
        [FromQuery] Guid? entityId,
        [FromQuery] string? action,
        [FromQuery] DateTimeOffset? fromUtc,
        [FromQuery] DateTimeOffset? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) => ScaffoldNotImplemented("ADM-011");

    /// <summary>ADM-012: Regenerate and resend initial Employee credential scaffold.</summary>
    [HttpPost("employees/{employeeId:guid}/resend-initial-credential")]
    public ActionResult<ApiErrorResponse> ResendInitialCredential(
        Guid employeeId,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("ADM-012");
}
