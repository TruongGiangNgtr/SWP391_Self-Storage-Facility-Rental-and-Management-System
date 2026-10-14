using Frms.Api.DTOs.Requests;
using Frms.Api.Mapping;
using Frms.Api.DTOs.Responses;
using Frms.Business.Services.Interfaces;
using Frms.Business.Validators;
using Frms.Business.Models.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[ApiController]
[Authorize(Roles = "BUSINESS_OPERATIONS_MANAGER")]
[Route("api/v1/business/policies")]
public sealed class BusinessPoliciesController(
    IPolicyService policyService) : ControllerBase {
    /// <summary>
    /// BOM-008: List Policy versions.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPolicies(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) {
        if (page < 1 || pageSize < 1 || pageSize > 100) {
            return ValidationError(
                "page/pageSize",
                "page must be >= 1 and pageSize must be between 1 and 100.");
        }

        var (items, totalItems) = await policyService.GetPagedAsync(
            page,
            pageSize,
            cancellationToken);

        var data = items
            .Select(x => x.ToSummary())
            .ToList();

        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)pageSize);

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
    /// BOM-009: Create new Policy version.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreatePolicyVersion(
        [FromBody] CreatePolicyVersionRequest request,
        CancellationToken cancellationToken = default) {
        var validation = PolicyVersionValidator.Validate(
            request.DepositTimeoutHours,
            request.ReservationVisitStartDay,
            request.ReservationVisitEndDay,
            request.MonthlyPaymentDueDay,
            request.OverdueStartDay,
            request.LateFeeDivisorDays,
            request.EarlyReturnWaiveFeeUntilDay);
        if (validation is { } error)
            return ValidationError(error.Field, error.Message);

        var policy = await policyService.CreateVersionAsync(
            request.DepositTimeoutHours,
            request.ReservationVisitStartDay,
            request.ReservationVisitEndDay,
            request.MonthlyPaymentDueDay,
            request.OverdueStartDay,
            request.LateFeeDivisorDays,
            request.EarlyReturnWaiveFeeUntilDay,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new {
                data = policy.ToSummary(),
                message = "Policy version created."
            });
    }

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
