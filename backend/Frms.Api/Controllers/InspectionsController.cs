using Frms.Api.Authorization;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Route("api/v1")]
public sealed class InspectionsController(IInspectionWorkflowService workflow) : ScaffoldControllerBase
{
    private static InspectionSummaryResponse ToSummary(InspectionResult r)
        => new(r.InspectionId, r.ContractId, r.StorageUnitId, r.VisitId,
            r.EmployeeId, r.Status, r.ConditionNote, r.CompletedAt);

    [Authorize(Roles = RoleNames.FacilityStaff + "," + RoleNames.FacilityManager)]
    [HttpGet("inspections")]
    [ProducesResponseType(typeof(PaginatedResponse<InspectionSummaryResponse>), 200)]
    public async Task<ActionResult<PaginatedResponse<InspectionSummaryResponse>>> ListInspections(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var r = await workflow.ListAsync(page, pageSize, cancellationToken);
        return Ok(new PaginatedResponse<InspectionSummaryResponse>(
            r.Items.Select(ToSummary).ToArray(),
            new PaginationResponse(r.Page, r.PageSize, r.TotalItems,
                r.TotalItems == 0 ? 0 : (int)Math.Ceiling(r.TotalItems / (double)r.PageSize))));
    }

    [Authorize(Roles = RoleNames.FacilityStaff + "," + RoleNames.FacilityManager)]
    [HttpGet("inspections/{inspectionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<InspectionDetailResponse>), 200)]
    public async Task<ActionResult<ApiResponse<InspectionDetailResponse>>> GetInspection(
        Guid inspectionId, CancellationToken cancellationToken)
    {
        var r = await workflow.GetAsync(inspectionId, cancellationToken);
        return Ok(new ApiResponse<InspectionDetailResponse>(new InspectionDetailResponse(
            r.Inspection.InspectionId, r.Inspection.ContractId,
            r.Inspection.StorageUnitId, r.Inspection.VisitId,
            r.Inspection.EmployeeId, r.Inspection.Status,
            r.Inspection.ConditionNote,
            r.Damages.Select(x => new InspectionDamageResponse(x.DamageRecordId,
                x.DamageTypeId, x.DamageAmount, x.Note, x.Status)).ToArray(),
            r.ExtraFees.Select(x => new InspectionExtraFeeResponse(x.ExtraFeeId,
                x.ExtraFeeTypeId, x.Amount, x.Reason)).ToArray(),
            r.Evidence.Select(x => new InspectionEvidenceResponse(x.InspectionEvidenceId,
                x.EvidenceType, x.CreatedAt)).ToArray(),
            r.Inspection.CompletedAt)));
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
    public ActionResult<ApiErrorResponse> DecideDamage(
        Guid damageRecordId, [FromBody] DecideDamageRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("INS-009");

    [Authorize(Roles = RoleNames.FacilityStaff + "," + RoleNames.FacilityManager)]
    [HttpGet("damage-types")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DamageTypeResponse>>), 200)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DamageTypeResponse>>>> ListDamageTypes(
        CancellationToken cancellationToken)
    {
        var r = await workflow.ListDamageTypesAsync(cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<DamageTypeResponse>>(
            r.Select(x => new DamageTypeResponse(x.DamageTypeId, x.Name,
                x.DefaultAmount, x.Status)).ToArray()));
    }
}
