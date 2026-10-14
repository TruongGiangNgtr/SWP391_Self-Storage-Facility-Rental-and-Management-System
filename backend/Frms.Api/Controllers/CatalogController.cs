using Frms.Business.Models.Results;
using Frms.Api.Authorization;
using Frms.Api.Mapping;
using Frms.Api.DTOs.Responses;
using Frms.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Authorize]
[Route("api/v1")]
public sealed class CatalogController(
    ICapacityService capacityService,
    IUnitTypeService unitTypeService,
    IFacilityAuthorizationService facilityAuthorizationService,
    IFacilityCatalogService facilityCatalogService)
    : ScaffoldControllerBase {

    /// <summary>CAT-001: Browse active Facilities scaffold.</summary>
    /// <summary>CAT-001: Browse active Facilities.</summary>
    [AllowAnonymous]
    [HttpGet("facilities")]
    [ProducesResponseType(
        typeof(PaginatedResponse<FacilitySummaryResponse>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> BrowseFacilities(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default,
        [FromQuery] string? location = null,
        [FromQuery] Guid? facilityId = null,
        [FromQuery] string? mode = null,
        [FromQuery] string? size = null,
        [FromQuery] Guid? unitTypeId = null,
        [FromQuery] string? startMonth = null,
        [FromQuery] string? endMonth = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null) {
        if (location is not null || facilityId.HasValue || mode is not null ||
            size is not null || unitTypeId.HasValue || startMonth is not null ||
            endMonth is not null || sortBy is not null || sortDirection is not null)
            return ScaffoldNotImplemented("CAT-001-SEARCH");
        var result =
            await facilityCatalogService.GetActiveFacilitiesAsync(
                page,
                pageSize,
                cancellationToken);

        var data = result.Items
            .Select(x => x.ToSummary())
            .ToArray();

        var totalPages =
            result.TotalItems == 0
                ? 0
                : (int)Math.Ceiling(
                    result.TotalItems /
                    (double)result.PageSize);

        return Ok(new {
            data,
            pagination = new {
                page = result.Page,
                pageSize = result.PageSize,
                totalItems = result.TotalItems,
                totalPages
            }
        });
    }

    /// <summary>CAT-002: Active Facility detail.</summary>
    [AllowAnonymous]
    [HttpGet("facilities/{facilityId:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<FacilitySummaryResponse>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFacility(
        Guid facilityId,
        CancellationToken cancellationToken) {
        var result =
            await facilityCatalogService.GetActiveFacilityAsync(
                facilityId,
                cancellationToken);

        return Ok(
            new ApiResponse<FacilitySummaryResponse>(
                result.ToSummary()));
    }

    /// <summary>
    /// CAT-004: Unit Type detail.
    /// UnitType catalogue metadata is owned by SSP-01.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("unit-types/{unitTypeId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UnitTypeSummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnitType(
        Guid unitTypeId,
        CancellationToken cancellationToken = default) {
        if (User.Identity?.IsAuthenticated != true)
            return ScaffoldNotImplemented("CAT-004-PUBLIC");
        if (!User.IsInRole(RoleNames.Customer) &&
            !User.IsInRole(RoleNames.FacilityStaff) &&
            !User.IsInRole(RoleNames.FacilityManager))
            return Forbid();
        var unitType = await unitTypeService.GetByIdAsync(
            unitTypeId,
            cancellationToken);

        if (unitType is null) {
            return NotFound(new {
                code = "RESOURCE_NOT_FOUND",
                message = "Unit Type was not found.",
                traceId = HttpContext.TraceIdentifier
            });
        }

        return Ok(new {
            data = unitType.ToSummary()
        });
    }

    /// <summary>
    /// CAT-003: Unit Types offered at a Facility
    /// with optional requested-period capacity.
    /// Capacity calculation is owned by SSP-02.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("facilities/{facilityId:guid}/unit-types")]
    [ProducesResponseType(
        typeof(PaginatedResponse<UnitTypeAvailabilityResponse>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> ListUnitTypes(
        Guid facilityId,
        [FromQuery] string? startMonth = null,
        [FromQuery] string? endMonth = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default,
        [FromQuery] string? view = null) {
        if (page < 1 || pageSize < 1 || pageSize > 100) {
            return BadRequest(new {
                code = "VALIDATION_ERROR",
                message = "Request validation failed.",
                traceId = HttpContext.TraceIdentifier
            });
        }

        if (view is not null && view != "public" && view != "operational")
            return BadRequest(new {
                code = "VALIDATION_ERROR",
                message = "Unsupported view; choose public or operational.",
                traceId = HttpContext.TraceIdentifier
            });
        if (view != "operational")
            return ScaffoldNotImplemented("CAT-003-PUBLIC");
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized();
        if (!User.IsInRole(RoleNames.Customer) &&
            !User.IsInRole(RoleNames.FacilityStaff) &&
            !User.IsInRole(RoleNames.FacilityManager))
            return Forbid();
        if (!User.IsInRole(RoleNames.Customer)) {
            await facilityAuthorizationService.EnsureSameFacilityAsync(
                facilityId, cancellationToken);
        }

        var result = await capacityService.ListUnitTypesAsync(
            facilityId,
            startMonth,
            endMonth,
            page,
            pageSize,
            cancellationToken);

        var data = result.Items
            .Select(x => x.ToAvailabilityResponse())
            .ToArray();

        var totalPages =
            result.TotalItems == 0
                ? 0
                : (int)Math.Ceiling(
                    result.TotalItems / (double)pageSize);

        return Ok(new {
            data,
            pagination = new {
                page,
                pageSize,
                totalItems = result.TotalItems,
                totalPages
            }
        });
    }
}
