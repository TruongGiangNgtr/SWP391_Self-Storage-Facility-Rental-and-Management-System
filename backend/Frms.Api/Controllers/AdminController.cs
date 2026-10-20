using Frms.Api.Authorization;
using Frms.Api.Mapping;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Frms.Business.Services.Interfaces;
using Frms.Business.Models.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Authorize(Roles = RoleNames.SystemAdministrator)]
[Route("api/v1/admin")]
public sealed class AdminController(IAdminUserService adminUserService) : ControllerBase
{
    /// <summary>
    /// ADM-001: List all Customer/Employee accounts.
    /// </summary>
    [HttpGet("users")]
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
            .Select(x => x.ToResponse())
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
    [HttpGet("users/{userAccountId:guid}")]
    public async Task<IActionResult> GetUser(
        Guid userAccountId,
        CancellationToken cancellationToken = default) {
        var row = await adminUserService.GetByIdAsync(
            userAccountId,
            cancellationToken);

        if (row is null)
            return ResourceNotFound();

        return Ok(new {
            data = row.Value.ToResponse()
        });
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
    
}
