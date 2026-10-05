using Frms.Api.Authorization;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Authorize]
[Route("api/v1")]
public sealed class CatalogController : ScaffoldControllerBase
{
    /// <summary>CAT-001: Browse active Facilities scaffold.</summary>
    [Authorize(Roles = RoleNames.Customer)]
    [HttpGet("facilities")]
    [ProducesResponseType(typeof(PaginatedResponse<FacilitySummaryResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiErrorResponse> BrowseFacilities(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) => ScaffoldNotImplemented("CAT-001");

    /// <summary>CAT-002: Facility detail scaffold.</summary>
    [Authorize(Roles = RoleNames.Customer)]
    [HttpGet("facilities/{facilityId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<FacilitySummaryResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiErrorResponse> GetFacility(
        Guid facilityId,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("CAT-002");

    /// <summary>CAT-003: Facility UnitTypes and optional capacity scaffold.</summary>
    [Authorize(Roles = RoleNames.Customer + "," + RoleNames.FacilityStaff + "," + RoleNames.FacilityManager)]
    [HttpGet("facilities/{facilityId:guid}/unit-types")]
    [ProducesResponseType(typeof(PaginatedResponse<UnitTypeAvailabilityResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiErrorResponse> GetFacilityUnitTypes(
        Guid facilityId,
        [FromQuery] string? startMonth,
        [FromQuery] string? endMonth,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) => ScaffoldNotImplemented("CAT-003");

    /// <summary>CAT-004: UnitType detail scaffold.</summary>
    [Authorize(Roles = RoleNames.Customer + "," + RoleNames.FacilityStaff + "," + RoleNames.FacilityManager)]
    [HttpGet("unit-types/{unitTypeId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UnitTypeAvailabilityResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiErrorResponse> GetUnitType(
        Guid unitTypeId,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("CAT-004");
}
