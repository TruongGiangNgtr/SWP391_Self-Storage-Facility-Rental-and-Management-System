using Frms.Api.Authorization;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Frms.Business.Models.Commands;
using Frms.Business.Services.Interfaces;
using Frms.Business.Models;

namespace Frms.Api.Controllers;

[Authorize(Roles = RoleNames.FacilityManager)]
[Route("api/v1")]
public sealed class StorageUnitsController(IStorageUnitService storageUnitService) : ScaffoldControllerBase
{
    /// <summary>UNIT-001: List StorageUnits of a Facility.</summary>
    [HttpGet("facilities/{facilityId:guid}/storage-units")]
    [ProducesResponseType(
        typeof(ApiResponse<IReadOnlyList<StorageUnitListItem>>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StorageUnitListItem>>>>
        GetStorageUnits(
            Guid facilityId,
            CancellationToken cancellationToken)
    {
        var items = await storageUnitService.ListByFacilityAsync(
            facilityId,
            cancellationToken);

        return Ok(
            new ApiResponse<IReadOnlyList<StorageUnitListItem>>(
                items,
                "StorageUnits retrieved."));
    }

    /// <summary>UNIT-002: Create StorageUnit.</summary>
    [HttpPost("facilities/{facilityId:guid}/storage-units")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateStorageUnit(
        Guid facilityId,
        [FromBody] CreateStorageUnitRequest request,
        CancellationToken cancellationToken)
    {
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

    /// <summary>UNIT-003: Get StorageUnit detail.</summary>
    [HttpGet("storage-units/{storageUnitId:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<StorageUnitListItem>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<StorageUnitListItem>>> GetStorageUnit(
        Guid storageUnitId,
        CancellationToken cancellationToken)
    {
        var item = await storageUnitService.GetByIdAsync(
            storageUnitId,
            cancellationToken);

        if (item is null)
        {
            return NotFound();
        }

        return Ok(
            new ApiResponse<StorageUnitListItem>(
                item,
                "StorageUnit retrieved."));
    }

    /// <summary>UNIT-004: Update StorageUnit.</summary>
    [HttpPatch("storage-units/{storageUnitId:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateStorageUnit(
        Guid storageUnitId,
        [FromBody] UpdateStorageUnitRequest request,
        CancellationToken cancellationToken)
    {
        await storageUnitService.UpdateAsync(
            storageUnitId,
            new UpdateStorageUnitCommand(
                request.UnitTypeId,
                request.LocationInfo),
            cancellationToken);

        return Ok(
            new ApiResponse<bool>(
                true,
                "StorageUnit updated."));
    }

    /// <summary>UNIT-005: Change StorageUnit status.</summary>
    [HttpPost("storage-units/{storageUnitId:guid}/status")]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> ChangeStorageUnitStatus(
        Guid storageUnitId,
        [FromBody] ChangeStorageUnitStatusRequest request,
        CancellationToken cancellationToken)
    {
        await storageUnitService.ChangeStatusAsync(
            storageUnitId,
            new ChangeStorageUnitStatusCommand(
                request.Status),
            cancellationToken);

        return Ok(
            new ApiResponse<bool>(
                true,
                "StorageUnit status updated."));
    }
}
