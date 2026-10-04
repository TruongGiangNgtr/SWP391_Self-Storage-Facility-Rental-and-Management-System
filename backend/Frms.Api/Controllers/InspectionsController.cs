using Frms.Api.Authorization;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Route("api/v1")]
public sealed class InspectionsController : ScaffoldControllerBase
{
    /// <summary>INS-001: List Facility-scoped Inspections scaffold.</summary>
    [Authorize(Roles = RoleNames.FacilityStaff + "," + RoleNames.FacilityManager)]
    [HttpGet("inspections")]
    public ActionResult<ApiErrorResponse> ListInspections(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) => ScaffoldNotImplemented("INS-001");

    /// <summary>INS-002: Inspection detail scaffold.</summary>
    [Authorize(Roles = RoleNames.FacilityStaff + "," + RoleNames.FacilityManager)]
    [HttpGet("inspections/{inspectionId:guid}")]
    public ActionResult<ApiErrorResponse> GetInspection(
        Guid inspectionId,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("INS-002");

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
    public ActionResult<ApiErrorResponse> DecideDamage(
        Guid damageRecordId,
        [FromBody] DecideDamageRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("INS-009");

    /// <summary>INS-010: List active seeded DamageTypes scaffold.</summary>
    [Authorize(Roles = RoleNames.FacilityStaff + "," + RoleNames.FacilityManager)]
    [HttpGet("damage-types")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DamageTypeResponse>>), StatusCodes.Status200OK)]
    public ActionResult<ApiErrorResponse> ListDamageTypes(CancellationToken cancellationToken) =>
        ScaffoldNotImplemented("INS-010");
}
