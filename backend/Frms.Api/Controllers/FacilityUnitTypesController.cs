using Frms.Api.Authorization;
using Frms.Api.DTOs.Responses;
using Frms.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Authorize(
    Roles =
        RoleNames.Customer + "," +
        RoleNames.FacilityStaff + "," +
        RoleNames.FacilityManager)]
[Route("api/v1/facilities/{facilityId:guid}/unit-types")]
public sealed class FacilityUnitTypesController(
    ICapacityService capacityService)
    : ScaffoldControllerBase
{
    /// <summary>CAT-003: Unit Types and requested-period capacity.</summary>
    [HttpGet]
    [ProducesResponseType(
        typeof(PaginatedResponse<UnitTypeAvailabilityResponse>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> ListUnitTypes(
        Guid facilityId,
        [FromQuery] string? startMonth = null,
        [FromQuery] string? endMonth = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
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
                            x.RequestedStartMonth.Value
                                .ToString("yyyy-MM"),
                            x.RequestedEndMonth!.Value
                                .ToString("yyyy-MM")),
                    x.AvailableCapacity))
            .ToArray();

        var totalPages =
            result.TotalItems == 0
                ? 0
                : (int)Math.Ceiling(
                    result.TotalItems /
                    (double)pageSize);

        return Ok(new
        {
            data,
            pagination = new
            {
                page,
                pageSize,
                totalItems = result.TotalItems,
                totalPages
            }
        });
    }
}