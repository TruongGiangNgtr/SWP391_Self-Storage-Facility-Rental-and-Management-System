using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Frms.Business.Services.Interfaces;
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
            .Select(Map)
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
        if (request.DepositTimeoutHours <= 0) {
            return ValidationError(
                "depositTimeoutHours",
                "Deposit timeout hours must be greater than 0.");
        }

        if (!IsValidDay(request.ReservationVisitStartDay)) {
            return ValidationError(
                "reservationVisitStartDay",
                "Reservation visit start day must be between 1 and 31.");
        }

        if (!IsValidDay(request.ReservationVisitEndDay)) {
            return ValidationError(
                "reservationVisitEndDay",
                "Reservation visit end day must be between 1 and 31.");
        }

        if (request.ReservationVisitStartDay >
            request.ReservationVisitEndDay) {
            return ValidationError(
                "reservationVisitEndDay",
                "Reservation visit end day must be greater than or equal to start day.");
        }

        if (!IsValidDay(request.MonthlyPaymentDueDay)) {
            return ValidationError(
                "monthlyPaymentDueDay",
                "Monthly payment due day must be between 1 and 31.");
        }

        if (!IsValidDay(request.OverdueStartDay)) {
            return ValidationError(
                "overdueStartDay",
                "Overdue start day must be between 1 and 31.");
        }

        if (request.LateFeeDivisorDays <= 0) {
            return ValidationError(
                "lateFeeDivisorDays",
                "Late fee divisor days must be greater than 0.");
        }

        if (!IsValidDay(request.EarlyReturnWaiveFeeUntilDay)) {
            return ValidationError(
                "earlyReturnWaiveFeeUntilDay",
                "Early return waive fee cutoff must be between 1 and 31.");
        }

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
                data = Map(policy),
                message = "Policy version created."
            });
    }

    private static bool IsValidDay(int value)
        => value is >= 1 and <= 31;

    private static PolicySummary Map(PolicyResult policy)
        => new(
            policy.PolicyId,
            policy.Version,
            policy.Status,
            policy.EffectiveFrom,
            policy.EffectiveTo,
            policy.DepositTimeoutHours,
            policy.ReservationVisitStartDay,
            policy.ReservationVisitEndDay,
            policy.MonthlyPaymentDueDay,
            policy.OverdueStartDay,
            policy.LateFeeDivisorDays,
            policy.EarlyReturnWaiveFeeUntilDay,
            policy.CreatedAt);

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
