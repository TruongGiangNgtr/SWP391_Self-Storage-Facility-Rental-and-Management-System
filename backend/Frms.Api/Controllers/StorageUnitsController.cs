using Frms.Api.Authorization;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Frms.Business.Models.Commands;
using Frms.Business.Services.Implementations;
using Frms.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Authorize(Roles = RoleNames.FacilityManager)]
[Route("api/v1")]
public sealed class StorageUnitsController(
    IStorageUnitService storageUnitService) : ScaffoldControllerBase {
    /// <summary>UNIT-001: List Facility StorageUnits.</summary>
    [HttpGet("facilities/{facilityId:guid}/storage-units")]
    [ProducesResponseType(
        typeof(PaginatedResponse<StorageUnitDetailResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<StorageUnitDetailResponse>>>
        ListStorageUnits(
            Guid facilityId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default) {
        var (items, totalCount) =
            await storageUnitService.ListByFacilityAsync(
                facilityId,
                page,
                pageSize,
                cancellationToken);

        var data = items
            .Select(x => new StorageUnitDetailResponse(
                x.StorageUnitId,
                x.FacilityId,
                x.UnitTypeId,
                x.UnitCode,
                x.LocationInfo,
                x.Status))
            .ToList();

        var totalPages = (int)Math.Ceiling(
            totalCount / (double)pageSize);

        return Ok(
            new PaginatedResponse<StorageUnitDetailResponse>(
                data,
                new PaginationResponse(
                    page,
                    pageSize,
                    totalCount,
                    totalPages)));
    }

    /// <summary>UNIT-002: Create StorageUnit.</summary>
    [HttpPost("facilities/{facilityId:guid}/storage-units")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateStorageUnit(
        Guid facilityId,
        [FromBody] CreateStorageUnitRequest request,
        CancellationToken cancellationToken) {
        var storageUnitId = await storageUnitService.CreateAsync(
            facilityId,
            new CreateStorageUnitCommand(
                request.UnitTypeId,
                request.UnitCode,
                request.LocationInfo),
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new ApiResponse<Guid>(
                storageUnitId,
                "StorageUnit created."));
    }

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
