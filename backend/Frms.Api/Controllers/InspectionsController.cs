using Frms.Api.Authorization;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Route("api/v1")]
public sealed class InspectionsController(IInspectionService inspectionService) : ScaffoldControllerBase
{
    /// <summary>INS-001: List Facility-scoped Inspections scaffold.</summary>
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

    /// <summary>INS-002: Inspection detail scaffold.</summary>
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

    /// <summary>INS-003: Atomic Inspection claim scaffold.</summary>
    [Authorize(Roles = RoleNames.FacilityStaff)]
    [HttpPost("inspections/{inspectionId:guid}/claim")]
    public ActionResult<ApiErrorResponse> ClaimInspection(
        Guid inspectionId,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("INS-003");

    /// <summary>INS-004: Record Damage scaffold.</summary>
    [Authorize(Roles = RoleNames.FacilityStaff)]
    [HttpPost("inspections/{inspectionId:guid}/damages")]
    public ActionResult<ApiErrorResponse> RecordDamage(
        Guid inspectionId,
        [FromBody] RecordDamageRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("INS-004");

    /// <summary>INS-005: Record ExtraFee scaffold.</summary>
    [Authorize(Roles = RoleNames.FacilityStaff)]
    [HttpPost("inspections/{inspectionId:guid}/extra-fees")]
    public ActionResult<ApiErrorResponse> RecordExtraFee(
        Guid inspectionId,
        [FromBody] RecordExtraFeeRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("INS-005");

    /// <summary>INS-006: Upload binary InspectionEvidence scaffold.</summary>
    [Authorize(Roles = RoleNames.FacilityStaff)]
    [Consumes("multipart/form-data")]
    [HttpPost("inspections/{inspectionId:guid}/evidence")]
    public ActionResult<ApiErrorResponse> UploadEvidence(
        Guid inspectionId,
        [FromForm] UploadInspectionEvidenceRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("INS-006");

    /// <summary>INS-007: Complete Inspection scaffold.</summary>
    [Authorize(Roles = RoleNames.FacilityStaff)]
    [HttpPost("inspections/{inspectionId:guid}/complete")]
    public ActionResult<ApiErrorResponse> CompleteInspection(
        Guid inspectionId,
        [FromBody] CompleteInspectionRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("INS-007");

    /// <summary>INS-009: Facility Manager Damage decision scaffold.</summary>
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

    /// <summary>INS-010: List active seeded DamageTypes scaffold.</summary>
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
