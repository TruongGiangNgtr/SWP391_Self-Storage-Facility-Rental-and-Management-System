using Frms.Api.Authorization;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Route("api/v1")]
public sealed class InspectionsController(IInspectionService inspectionService) : ScaffoldControllerBase
{
    private static InspectionSummaryResponse ToSummary(InspectionResult r)
        => new(r.InspectionId, r.ContractId, r.StorageUnitId, r.VisitId,
            r.EmployeeId, r.Status, r.ConditionNote, r.CompletedAt);

    [Authorize(Roles = RoleNames.FacilityStaff + "," + RoleNames.FacilityManager)]
    [HttpGet("inspections")]
    public async Task<IActionResult> ListInspections(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await inspectionService.ListAccessibleAsync(page, pageSize, cancellationToken);
        var data = result.Items.Select(ToSummaryResponse).ToArray();
        var totalPages = result.TotalItems == 0 ? 0 : (int)Math.Ceiling(result.TotalItems / (double)pageSize);
        return Ok(new {
            data,
            pagination = new { page, pageSize, totalItems = result.TotalItems, totalPages }
        });
    }

    [Authorize(Roles = RoleNames.FacilityStaff + "," + RoleNames.FacilityManager)]
    [HttpGet("inspections/{inspectionId:guid}")]
    public async Task<ActionResult<ApiResponse<InspectionDetailResponse>>> GetInspection(
        Guid inspectionId,
        CancellationToken cancellationToken)
    {
        var result = await inspectionService.GetAccessibleAsync(inspectionId, cancellationToken);
        return Ok(new ApiResponse<InspectionDetailResponse>(new InspectionDetailResponse(
            ToSummaryResponse(result.Inspection),
            result.Damages.Select(x => new InspectionDamageResponse(
                x.DamageRecordId, x.DamageTypeId, x.DamageTypeName,
                x.DamageAmount, x.Note, x.Status, x.CreatedAt)).ToArray(),
            result.ExtraFees.Select(x => new InspectionExtraFeeResponse(
                x.ExtraFeeId, x.ExtraFeeTypeId, x.Amount, x.Reason, x.CreatedAt)).ToArray(),
            result.Evidence.Select(x => new InspectionEvidenceMetadataResponse(
                x.InspectionEvidenceId, x.EvidenceType, x.CreatedAt)).ToArray())));
    }

    [Authorize(Roles = RoleNames.FacilityStaff)]
    [HttpPost("inspections/{inspectionId:guid}/claim")]
    [ProducesResponseType(typeof(ApiResponse<InspectionSummaryResponse>), 200)]
    public async Task<ActionResult<ApiResponse<InspectionSummaryResponse>>> ClaimInspection(
        Guid inspectionId, CancellationToken cancellationToken)
    {
        var r = await workflow.ClaimAsync(inspectionId, cancellationToken);
        return Ok(new ApiResponse<InspectionSummaryResponse>(ToSummary(r)));
    }

    [Authorize(Roles = RoleNames.FacilityStaff)]
    [HttpPost("inspections/{inspectionId:guid}/damages")]
    [ProducesResponseType(typeof(ApiResponse<InspectionDamageResponse>), 201)]
    public async Task<ActionResult<ApiResponse<InspectionDamageResponse>>> RecordDamage(
        Guid inspectionId, [FromBody] RecordDamageRequest request,
        CancellationToken cancellationToken)
    {
        var r = await workflow.RecordDamageAsync(inspectionId, request.DamageTypeId,
            request.DamageAmount, request.Note, cancellationToken);
        return StatusCode(201, new ApiResponse<InspectionDamageResponse>(
            new(r.DamageRecordId, r.DamageTypeId, r.DamageAmount, r.Note, r.Status)));
    }

    [Authorize(Roles = RoleNames.FacilityStaff)]
    [HttpPost("inspections/{inspectionId:guid}/extra-fees")]
    [ProducesResponseType(typeof(ApiResponse<InspectionExtraFeeResponse>), 201)]
    public async Task<ActionResult<ApiResponse<InspectionExtraFeeResponse>>> RecordExtraFee(
        Guid inspectionId, [FromBody] RecordExtraFeeRequest request,
        CancellationToken cancellationToken)
    {
        var r = await workflow.RecordExtraFeeAsync(inspectionId, request.ExtraFeeTypeId,
            request.Amount, request.Reason, cancellationToken);
        return StatusCode(201, new ApiResponse<InspectionExtraFeeResponse>(
            new(r.ExtraFeeId, r.ExtraFeeTypeId, r.Amount, r.Reason)));
    }

    [Authorize(Roles = RoleNames.FacilityStaff)]
    [HttpPost("inspections/{inspectionId:guid}/evidence")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10_000_000)] // Technical demo limit; review for deployment.
    [ProducesResponseType(typeof(ApiResponse<InspectionEvidenceResponse>), 201)]
    public async Task<ActionResult<ApiResponse<InspectionEvidenceResponse>>> UploadEvidence(
        Guid inspectionId, [FromForm] UploadInspectionEvidenceRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File.Length == 0 || request.File.Length > 9_000_000)
            return BadRequest(new ApiErrorResponse("VALIDATION_ERROR",
                "Evidence must be between 1 and 9 MB.", HttpContext.TraceIdentifier));
        await using var input = request.File.OpenReadStream();
        await using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, cancellationToken);
        var r = await workflow.AddEvidenceAsync(inspectionId, buffer.ToArray(),
            request.EvidenceType, cancellationToken);
        return StatusCode(201, new ApiResponse<InspectionEvidenceResponse>(
            new(r.InspectionEvidenceId, r.EvidenceType, r.CreatedAt)));
    }

    [Authorize(Roles = RoleNames.FacilityStaff)]
    [HttpPost("inspections/{inspectionId:guid}/complete")]
    [ProducesResponseType(typeof(ApiResponse<InspectionSummaryResponse>), 200)]
    public async Task<ActionResult<ApiResponse<InspectionSummaryResponse>>> CompleteInspection(
        Guid inspectionId, [FromBody] CompleteInspectionRequest request,
        CancellationToken cancellationToken)
    {
        var r = await workflow.CompleteAsync(inspectionId, request.ConditionNote, cancellationToken);
        return Ok(new ApiResponse<InspectionSummaryResponse>(ToSummary(r)));
    }

    // MWP-04 only. Intentionally not implemented in FWP-05/06/07 scope.
    [Authorize(Roles = RoleNames.FacilityManager)]
    [HttpPost("damage-records/{damageRecordId:guid}/decision")]
    [ProducesResponseType(typeof(ApiResponse<DamageDecisionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DamageDecisionResponse>>> DecideDamage(
        Guid damageRecordId,
        [FromBody] DecideDamageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await inspectionService.DecideDamageAsync(
            damageRecordId, request.Decision, cancellationToken);
        return Ok(new ApiResponse<DamageDecisionResponse>(
            new DamageDecisionResponse(result.DamageRecordId, result.Status),
            "Damage decision recorded."));
    }

    [Authorize(Roles = RoleNames.FacilityStaff + "," + RoleNames.FacilityManager)]
    [HttpGet("damage-types")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DamageTypeResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DamageTypeResponse>>>> ListDamageTypes(
        CancellationToken cancellationToken)
    {
        var types = await inspectionService.ListActiveDamageTypesAsync(cancellationToken);
        var response = types.Select(x => new DamageTypeResponse(
            x.DamageTypeId, x.Name, x.DefaultAmount, x.Status)).ToArray();
        return Ok(new ApiResponse<IReadOnlyList<DamageTypeResponse>>(response));
    }

    private static InspectionSummaryResponse ToSummaryResponse(InspectionSummaryRecord x) => new(
        x.InspectionId, x.ContractId, x.StorageUnitId, x.VisitId,
        x.EmployeeId, x.FacilityId, x.Status, x.ConditionNote, x.CompletedAt);
}
