using Frms.Api.Authorization;
using Frms.Api.Mapping;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Frms.Business.Models.Commands;
using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Frms.Business.Models;

namespace Frms.Api.Controllers;

[Authorize]
[Route("api/v1")]
public sealed class StorageUnitsController(
    IStorageUnitService storageUnitService)
    : ScaffoldControllerBase {
    /// <summary>UNIT-001: List Facility StorageUnits.</summary>
    [Authorize(Roles = RoleNames.FacilityManager)]
    [HttpGet("facilities/{facilityId:guid}/storage-units")]
    [ProducesResponseType(
        typeof(PaginatedResponse<StorageUnitDetailResponse>),
        StatusCodes.Status200OK)]
    public async Task<
        ActionResult<PaginatedResponse<StorageUnitDetailResponse>>>
        ListStorageUnits(
            Guid facilityId,
            [FromQuery] Guid? unitTypeId = null,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default) {
        var (items, totalCount) =
            await storageUnitService.ListByFacilityAsync(
                facilityId,
                unitTypeId,
                status,
                page,
                pageSize,
                cancellationToken);

        var data = items
            .Select(x => x.ToResponse())
            .ToList();

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(
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
    [Authorize(Roles = RoleNames.FacilityManager)]
    [HttpPost("facilities/{facilityId:guid}/storage-units")]
    [ProducesResponseType(
        typeof(ApiResponse<Guid>),
        StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<Guid>>>
        CreateStorageUnit(
            Guid facilityId,
            [FromBody] CreateStorageUnitRequest request,
            CancellationToken cancellationToken) {
        var storageUnitId =
            await storageUnitService.CreateAsync(
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

    /// <summary>UNIT-003: StorageUnit detail.</summary>
    [Authorize(Roles = RoleNames.FacilityManager)]
    [HttpGet("storage-units/{storageUnitId:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<StorageUnitDetailResponse>),
        StatusCodes.Status200OK)]
    public async Task<
        ActionResult<ApiResponse<StorageUnitDetailResponse>>>
        GetStorageUnit(
            Guid storageUnitId,
            CancellationToken cancellationToken) {
        var result =
            await storageUnitService.GetByIdAsync(
                storageUnitId,
                cancellationToken);

        return Ok(
            new ApiResponse<StorageUnitDetailResponse>(
                result.ToResponse()));
    }

    /// <summary>UNIT-004: Update allowed StorageUnit fields.</summary>
    [Authorize(Roles = RoleNames.FacilityManager)]
    [HttpPatch("storage-units/{storageUnitId:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<StorageUnitDetailResponse>),
        StatusCodes.Status200OK)]
    public async Task<
        ActionResult<ApiResponse<StorageUnitDetailResponse>>>
        UpdateStorageUnit(
            Guid storageUnitId,
            [FromBody] UpdateStorageUnitRequest request,
            CancellationToken cancellationToken) {
        var result =
            await storageUnitService.UpdateAsync(
                storageUnitId,
                new UpdateStorageUnitCommand(
                    request.UnitTypeId,
                    request.LocationInfo),
                cancellationToken);

        return Ok(
            new ApiResponse<StorageUnitDetailResponse>(
                result.ToResponse(),
                "StorageUnit updated."));
    }

    /// <summary>UNIT-005: Operational StorageUnit status transition.</summary>
    [Authorize(Roles = RoleNames.FacilityManager)]
    [HttpPost("storage-units/{storageUnitId:guid}/status")]
    [ProducesResponseType(
        typeof(ApiResponse<StorageUnitDetailResponse>),
        StatusCodes.Status200OK)]
    public async Task<
        ActionResult<ApiResponse<StorageUnitDetailResponse>>>
        ChangeStorageUnitStatus(
            Guid storageUnitId,
            [FromBody] ChangeStorageUnitStatusRequest request,
            CancellationToken cancellationToken) {
        var result =
            await storageUnitService.ChangeStatusAsync(
                storageUnitId,
                new ChangeStorageUnitStatusCommand(
                    request.Status),
                cancellationToken);

        return Ok(
            new ApiResponse<StorageUnitDetailResponse>(
                result.ToResponse(),
                "StorageUnit status updated."));
    }
}
