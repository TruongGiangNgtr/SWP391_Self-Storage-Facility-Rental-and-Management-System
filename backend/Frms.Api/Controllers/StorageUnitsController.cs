using Frms.Api.Authorization;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Authorize(Roles = RoleNames.FacilityManager)]
[Route("api/v1")]
public sealed class StorageUnitsController : ScaffoldControllerBase
{
    /// <summary>UNIT-001: List Facility StorageUnits scaffold.</summary>
    [HttpGet("facilities/{facilityId:guid}/storage-units")]
    public ActionResult<ApiErrorResponse> ListStorageUnits(
        Guid facilityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) => ScaffoldNotImplemented("UNIT-001");

    /// <summary>UNIT-002: Create StorageUnit scaffold.</summary>
    [HttpPost("facilities/{facilityId:guid}/storage-units")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<ApiErrorResponse> CreateStorageUnit(
        Guid facilityId,
        [FromBody] CreateStorageUnitRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("UNIT-002");

    /// <summary>UNIT-003: StorageUnit detail scaffold.</summary>
    [HttpGet("storage-units/{storageUnitId:guid}")]
    public ActionResult<ApiErrorResponse> GetStorageUnit(
        Guid storageUnitId,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("UNIT-003");

    /// <summary>UNIT-004: Update allowed StorageUnit fields scaffold.</summary>
    [HttpPatch("storage-units/{storageUnitId:guid}")]
    public ActionResult<ApiErrorResponse> UpdateStorageUnit(
        Guid storageUnitId,
        [FromBody] UpdateStorageUnitRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("UNIT-004");

    /// <summary>UNIT-005: Operational StorageUnit status transition scaffold.</summary>
    [HttpPost("storage-units/{storageUnitId:guid}/status")]
    public ActionResult<ApiErrorResponse> ChangeStorageUnitStatus(
        Guid storageUnitId,
        [FromBody] ChangeStorageUnitStatusRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("UNIT-005");
}
