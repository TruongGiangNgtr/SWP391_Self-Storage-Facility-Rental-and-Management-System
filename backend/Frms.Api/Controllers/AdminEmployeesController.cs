using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Frms.Business.Exceptions;
using Frms.Business.Models.Commands;
using Frms.Business.Services.Interfaces;
using Frms.Business.Models.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[ApiController]
[Authorize(Roles = "SYSTEM_ADMINISTRATOR")]
[Route("api/v1/admin/employees")]
public sealed class AdminEmployeesController(
    IAdminEmployeeService adminEmployeeService) : ControllerBase {
    /// <summary>
    /// ADM-005: Create Employee and provision initial credential.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateEmployee(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken cancellationToken = default) {
        try {
            var result = await adminEmployeeService.CreateAsync(
                new CreateAdminEmployeeCommand(
                    request.FullName,
                    request.Email,
                    request.PhoneNumber,
                    request.Role,
                    request.FacilityId),
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                new {
                    data = Map(result),
                    message = "Employee created."
                });
        }
        catch (AdminEmployeeOperationException ex) {
            return MapError(ex);
        }
    }

    /// <summary>
    /// ADM-006: Update Employee basic profile.
    /// </summary>
    [HttpPatch("{employeeId:guid}")]
    public async Task<IActionResult> UpdateEmployee(
        Guid employeeId,
        [FromBody] UpdateAdminEmployeeRequest request,
        CancellationToken cancellationToken = default) {
        try {
            var result = await adminEmployeeService.UpdateAsync(
                employeeId,
                new UpdateAdminEmployeeCommand(
                    request.FullName),
                cancellationToken);

            if (result is null)
                return ResourceNotFound();

            return Ok(new {
                data = Map(result.Value),
                message = "Employee updated."
            });
        }
        catch (AdminEmployeeOperationException ex) {
            return MapError(ex);
        }
    }

    /// <summary>
    /// ADM-007: Activate Employee.
    /// </summary>
    [HttpPost("{employeeId:guid}/activate")]
    public async Task<IActionResult> ActivateEmployee(
        Guid employeeId,
        CancellationToken cancellationToken = default) {
        var result = await adminEmployeeService.ActivateAsync(
            employeeId,
            cancellationToken);

        if (result is null)
            return ResourceNotFound();

        return Ok(new {
            data = Map(result.Value),
            message = "Employee activated."
        });
    }

    /// <summary>
    /// ADM-008: Deactivate Employee.
    /// </summary>
    [HttpPost("{employeeId:guid}/deactivate")]
    public async Task<IActionResult> DeactivateEmployee(
        Guid employeeId,
        CancellationToken cancellationToken = default) {
        var result = await adminEmployeeService.DeactivateAsync(
            employeeId,
            cancellationToken);

        if (result is null)
            return ResourceNotFound();

        return Ok(new {
            data = Map(result.Value),
            message = "Employee deactivated."
        });
    }

    /// <summary>
    /// ADM-012: Regenerate and resend initial credential.
    /// </summary>
    [HttpPost("{employeeId:guid}/resend-initial-credential")]
    public async Task<IActionResult> ResendInitialCredential(
        Guid employeeId,
        CancellationToken cancellationToken = default) {
        try {
            var result =
                await adminEmployeeService.ResendInitialCredentialAsync(
                    employeeId,
                    cancellationToken);

            if (result is null)
                return ResourceNotFound();

            return Ok(new {
                data = Map(result.Value),
                message = "Initial credential sent."
            });
        }
        catch (AdminEmployeeOperationException ex) {
            return MapError(ex);
        }
    }

    private static AdminUserAccountResponse Map(
        (
            AccountProfileResult Account,
            EmployeeProfileResult Employee,
            string RoleName) row)
        => new(
            row.Account.UserAccountId,
            row.RoleName,
            row.Account.Status,
            row.Account.Email,
            row.Account.PhoneNumber,
            row.Account.CreatedAt,
            new AdminUserProfileResponse(
                CustomerId: null,
                EmployeeId: row.Employee.EmployeeId,
                FullName: row.Employee.FullName,
                Address: null,
                Cccd: null,
                FacilityId: row.Employee.FacilityId));

    private IActionResult MapError(
        AdminEmployeeOperationException ex) {
        if (ex.Code == "VALIDATION_ERROR") {
            return BadRequest(new {
                code = ex.Code,
                message = "Request validation failed.",
                errors = ex.Field is null
                    ? null
                    : new Dictionary<string, string[]> {
                        [ex.Field] = [ex.Message]
                    },
                traceId = HttpContext.TraceIdentifier
            });
        }

        if (ex.Code is
            "EMAIL_ALREADY_EXISTS" or
            "PHONE_NUMBER_ALREADY_EXISTS" or
            "CREDENTIAL_PROVISIONING_INVALID_STATUS") {
            return Conflict(new {
                code = ex.Code,
                message = ex.Message,
                traceId = HttpContext.TraceIdentifier
            });
        }

        if (ex.Code == "RESOURCE_NOT_FOUND") {
            return NotFound(new {
                code = ex.Code,
                message = ex.Message,
                traceId = HttpContext.TraceIdentifier
            });
        }

        if (ex.Code == "EXTERNAL_PROVIDER_UNAVAILABLE") {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new {
                    code = ex.Code,
                    message = ex.Message,
                    traceId = HttpContext.TraceIdentifier
                });
        }

        throw ex;
    }

    private ObjectResult ResourceNotFound()
        => NotFound(new {
            code = "RESOURCE_NOT_FOUND",
            message = "Employee was not found.",
            traceId = HttpContext.TraceIdentifier
        });

    /// <summary>
    /// ADM-009: Assign Employee role and Facility.
    /// </summary>
    [HttpPut("{employeeId:guid}/assignment")]
    public async Task<IActionResult> AssignEmployee(
        Guid employeeId,
        [FromBody] AssignEmployeeRequest request,
        CancellationToken cancellationToken = default) {
        try {
            var result = await adminEmployeeService.AssignAsync(
                employeeId,
                request.Role,
                request.FacilityId,
                cancellationToken);

            if (result is null)
                return ResourceNotFound();

            return Ok(new {
                data = Map(result.Value),
                message = "Employee assignment updated."
            });
        }
        catch (AdminEmployeeOperationException ex) {
            return MapError(ex);
        }
    }
}
