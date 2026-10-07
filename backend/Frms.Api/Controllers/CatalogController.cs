using Frms.Api.Authorization;
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
    [Authorize(Roles = RoleNames.Customer)]
    [HttpGet("facilities")]
    [ProducesResponseType(
        typeof(PaginatedResponse<FacilitySummaryResponse>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> BrowseFacilities(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) {
        var result =
            await facilityCatalogService.GetActiveFacilitiesAsync(
                page,
                pageSize,
                cancellationToken);

        var data = result.Items
            .Select(MapFacility)
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
    [Authorize(Roles = RoleNames.Customer)]
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
                MapFacility(result)));
    }

    /// <summary>
    /// CAT-004: Unit Type detail.
    /// UnitType catalogue metadata is owned by SSP-01.
    /// </summary>
    [Authorize(
        Roles =
            RoleNames.Customer + "," +
            RoleNames.FacilityStaff + "," +
            RoleNames.FacilityManager)]
    [HttpGet("unit-types/{unitTypeId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UnitTypeSummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnitType(
        Guid unitTypeId,
        CancellationToken cancellationToken = default) {
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
            data = Map(unitType)
        });
    }

    private static UnitTypeSummary Map(
        Frms.DataAccess.Persistence.Entities.UnitType unitType)
        => new(
            unitType.UnitTypeId,
            unitType.Name,
            unitType.Mode,
            unitType.Size,
            unitType.RentalPrice,
            unitType.Description);

    /// <summary>
    /// CAT-003: Unit Types offered at a Facility
    /// with optional requested-period capacity.
    /// Capacity calculation is owned by SSP-02.
    /// </summary>
    [Authorize(
        Roles =
            RoleNames.Customer + "," +
            RoleNames.FacilityStaff + "," +
            RoleNames.FacilityManager)]
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
        CancellationToken cancellationToken = default) {
        if (page < 1 || pageSize < 1 || pageSize > 100) {
            return BadRequest(new {
                code = "VALIDATION_ERROR",
                message = "Request validation failed.",
                traceId = HttpContext.TraceIdentifier
            });
        }

        // Customer may browse ACTIVE facilities.
        // Staff/Manager must be assigned to the path Facility.
        if (!User.IsInRole(RoleNames.Customer)) {
            await facilityAuthorizationService.EnsureSameFacilityAsync(
                facilityId,
                cancellationToken);
        }

        var result = await capacityService.ListUnitTypesAsync(
            facilityId,
            startMonth,
            endMonth,
            page,
            pageSize,
            cancellationToken);

        var data = result.Items
            .Select(x =>
                new UnitTypeAvailabilityResponse(
                    x.UnitTypeId,
                    x.Name,
                    x.Mode,
                    x.Size,
                    x.RentalPrice,
                    x.Description,
                    x.RequestedStartMonth is null
                        ? null
                        : new RequestedPeriodResponse(
                            x.RequestedStartMonth.Value.ToString("yyyy-MM"),
                            x.RequestedEndMonth!.Value.ToString("yyyy-MM")),
                    x.AvailableCapacity))
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

    private static FacilitySummaryResponse MapFacility(
        Frms.Business.Models.Results.FacilityCatalogResult facility)
        => new(
            facility.FacilityId,
            facility.Name,
            facility.Address,
            facility.ContactInfo,
            facility.Description,
            facility.Status);
}
