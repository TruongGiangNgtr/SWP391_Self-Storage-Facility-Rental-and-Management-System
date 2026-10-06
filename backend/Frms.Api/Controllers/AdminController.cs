using Frms.Api.Authorization;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Persistence.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Authorize(Roles = RoleNames.SystemAdministrator)]
[Route("api/v1/admin")]
public sealed class AdminController(
    IAdminUserService adminUserService) : ScaffoldControllerBase
{
    /// <summary>
    /// ADM-001: List all Customer/Employee accounts.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) {
        if (page < 1 || pageSize < 1 || pageSize > 100) {
            return ValidationError(
                "page/pageSize",
                "page must be >= 1 and pageSize must be between 1 and 100.");
        }

        var (items, totalItems) =
            await adminUserService.GetPagedAsync(
                page,
                pageSize,
                cancellationToken);

        var data = items
            .Select(Map)
            .ToList();

        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(
                totalItems / (double)pageSize);

        return Ok(new {
            data,
            pagination = new {
                page,
                pageSize,
                totalItems,
                totalPages
            }
        });
    }

    /// <summary>
    /// ADM-002: User/account profile detail.
    /// </summary>
    [HttpGet("{userAccountId:guid}")]
    public async Task<IActionResult> GetUser(
        Guid userAccountId,
        CancellationToken cancellationToken = default) {
        var row = await adminUserService.GetByIdAsync(
            userAccountId,
            cancellationToken);

        if (row is null)
            return ResourceNotFound();

        return Ok(new {
            data = Map(row.Value)
        });
    }

    private static AdminUserAccountResponse Map(
        (
            UserAccount Account,
            string RoleName,
            Customer? Customer,
            Employee? Employee) row) {
        AdminUserProfileResponse? profile = null;

        if (row.Customer is not null) {
            profile = new AdminUserProfileResponse(
                CustomerId: row.Customer.CustomerId,
                EmployeeId: null,
                FullName: row.Customer.FullName,
                Address: row.Customer.Address,
                Cccd: row.Customer.Cccd,
                FacilityId: null);
        }
        else if (row.Employee is not null) {
            profile = new AdminUserProfileResponse(
                CustomerId: null,
                EmployeeId: row.Employee.EmployeeId,
                FullName: row.Employee.FullName,
                Address: null,
                Cccd: null,
                FacilityId: row.Employee.FacilityId);
        }

        return new AdminUserAccountResponse(
            row.Account.UserAccountId,
            row.RoleName,
            row.Account.Status,
            row.Account.Email,
            row.Account.PhoneNumber,
            row.Account.CreatedAt,
            profile);
    }

    private ObjectResult ResourceNotFound()
        => NotFound(new {
            code = "RESOURCE_NOT_FOUND",
            message = "User account was not found.",
            traceId = HttpContext.TraceIdentifier
        });

    private BadRequestObjectResult ValidationError(
        string field,
        string message)
        => BadRequest(new {
            code = "VALIDATION_ERROR",
            message = "Request validation failed.",
            errors = new Dictionary<string, string[]> {
                [field] = [message]
            },
            traceId = HttpContext.TraceIdentifier
        });

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
