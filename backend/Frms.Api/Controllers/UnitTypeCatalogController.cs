using Frms.Api.DTOs.Responses;
using Frms.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class UnitTypeCatalogController(
    IUnitTypeService unitTypeService,
    IFacilityAuthorizationService facilityAuthorizationService)
    : ControllerBase {
    /// <summary>
    /// CAT-003: Unit Types offered at a Facility.
    /// Capacity query is implemented under SSP-02.
    /// </summary>
    [Authorize(
        Roles = "CUSTOMER,FACILITY_STAFF,FACILITY_MANAGER")]
    [HttpGet("facilities/{facilityId:guid}/unit-types")]
    public async Task<IActionResult> GetFacilityUnitTypes(
        Guid facilityId,
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

        var isCustomer = User.IsInRole("CUSTOMER");

        if (!isCustomer) {
            await facilityAuthorizationService
                .EnsureSameFacilityAsync(
                    facilityId,
                    cancellationToken);
        }

        var (items, totalItems, facilityStatus) =
            await unitTypeService.GetFacilityPagedAsync(
                facilityId,
                page,
                pageSize,
                cancellationToken);

        if (facilityStatus is null) {
            return NotFound(new {
                code = "RESOURCE_NOT_FOUND",
                message = "Facility was not found.",
                traceId = HttpContext.TraceIdentifier
            });
        }

        if (isCustomer &&
            facilityStatus != "ACTIVE") {
            return Conflict(new {
                code = "FACILITY_INACTIVE",
                message = "Facility is inactive.",
                traceId = HttpContext.TraceIdentifier
            });
        }

        var data = items
            .Select(Map)
            .ToList();

        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(
                totalItems / (double)pageSize);

        return Ok(new {
            data,
            pagination = new {
                page,
                pageSize,
                totalItems,
                totalPages
            }
        });
    }

    /// <summary>
    /// CAT-004: Unit Type detail.
    /// </summary>
    [Authorize(
        Roles = "CUSTOMER,FACILITY_STAFF,FACILITY_MANAGER")]
    [HttpGet("unit-types/{unitTypeId:guid}")]
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
}
