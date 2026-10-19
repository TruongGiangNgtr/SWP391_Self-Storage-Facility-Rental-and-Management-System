using Frms.Api.Authorization;
using Frms.Api.DTOs.Responses;
using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

/// <summary>Read-only, evidence-based StorageUnit operational status history.</summary>
[Authorize(Roles = RoleNames.FacilityManager + "," + RoleNames.BusinessOperationsManager + "," + RoleNames.SystemAdministrator)]
[Route("api/v1/facilities/{facilityId:guid}")]
public sealed class StorageUnitTimelineController(IStorageUnitTimelineService service) : ScaffoldControllerBase
{
    [HttpGet("storage-units/timeline")]
    [ProducesResponseType(typeof(PaginatedResponse<StorageUnitTimelineResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<StorageUnitTimelineResponse>>> ByFacility(
        Guid facilityId, [FromQuery] string? month, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var (items, total) = await service.ListAsync(facilityId, null, month, page, pageSize, cancellationToken);
        return Ok(Page(items, total, page, pageSize));
    }

    [HttpGet("storage-units/{storageUnitId:guid}/timeline")]
    [ProducesResponseType(typeof(ApiResponse<StorageUnitTimelineResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<StorageUnitTimelineResponse>>> ByStorageUnit(
        Guid facilityId, Guid storageUnitId, [FromQuery] string? month,
        CancellationToken cancellationToken = default)
    {
        var item = await service.GetAsync(facilityId, storageUnitId, month, cancellationToken);
        return Ok(new ApiResponse<StorageUnitTimelineResponse>(ToResponse(item)));
    }

    [HttpGet("unit-types/{unitTypeId:guid}/storage-units/timeline")]
    [ProducesResponseType(typeof(PaginatedResponse<StorageUnitTimelineResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<StorageUnitTimelineResponse>>> ByUnitType(
        Guid facilityId, Guid unitTypeId, [FromQuery] string? month,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await service.ListAsync(facilityId, unitTypeId, month, page, pageSize, cancellationToken);
        return Ok(Page(items, total, page, pageSize));
    }

    private static PaginatedResponse<StorageUnitTimelineResponse> Page(
        IReadOnlyList<StorageUnitTimelineResult> items, int total, int page, int pageSize) =>
        new(items.Select(ToResponse).ToArray(),
            new PaginationResponse(page, pageSize, total,
                total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize)));

    private static StorageUnitTimelineResponse ToResponse(StorageUnitTimelineResult item) =>
        new(item.StorageUnitId, item.UnitCode, item.UnitTypeName, item.FacilityName,
            item.LocationInfo, item.Intervals.Select(x =>
                new StorageUnitStatusIntervalResponse(x.Status, x.From, x.To)).ToArray());
}
