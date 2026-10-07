using Frms.Api.Authorization;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Frms.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Authorize(Roles = RoleNames.Customer)]
[Route("api/v1/contracts")]
public sealed class ContractAccessVisitsController(
    IVisitService visitService)
    : ScaffoldControllerBase
{
    /// <summary>VIS-001: Create ACCESS Visit.</summary>
    [HttpPost("{contractId:guid}/access-visits")]
    [ProducesResponseType(
        typeof(ApiResponse<VisitDetailResponse>),
        StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<VisitDetailResponse>>> CreateAccessVisit(
        Guid contractId,
        [FromBody] CreateAccessVisitRequest request,
        CancellationToken cancellationToken)
    {
        var visit =
            await visitService.CreateAccessAsync(
                contractId,
                request.VisitDate,
                cancellationToken);

        var response =
            new VisitDetailResponse(
                visit.VisitId,
                visit.EntityId,
                visit.VisitType,
                visit.VisitDate,
                visit.ActualReturnDate.HasValue
                    ? DateOnly.FromDateTime(
                        visit.ActualReturnDate.Value)
                    : null,
                visit.Status,
                visit.EmployeeId);

        return StatusCode(
            StatusCodes.Status201Created,
            new ApiResponse<VisitDetailResponse>(
                response,
                "ACCESS Visit created."));
    }
}