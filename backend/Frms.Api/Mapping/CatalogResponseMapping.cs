using Frms.Api.DTOs.Responses;
using Frms.Business.Models.Results;

namespace Frms.Api.Mapping;

internal static class CatalogResponseMapping
{
    public static FacilitySummaryResponse ToSummary(this FacilityCatalogResult facility) => new(
        facility.FacilityId, facility.Name, facility.Address, facility.ContactInfo,
        facility.Description, facility.Status);

    public static FacilitySummary ToSummary(this FacilityResult facility) => new(
        facility.FacilityId, facility.Name, facility.Address, facility.ContactInfo,
        facility.Description, facility.Status);

    public static UnitTypeSummary ToSummary(this UnitTypeResult unitType) => new(
        unitType.UnitTypeId, unitType.Name, unitType.Mode, unitType.Size,
        unitType.RentalPrice, unitType.Description, unitType.ImageUrl, unitType.VideoUrl);

    public static UnitTypeAvailabilityResponse ToAvailabilityResponse(this UnitTypeAvailabilityResult unitType) => new(
        unitType.UnitTypeId, unitType.Name, unitType.Mode, unitType.Size,
        unitType.RentalPrice, unitType.Description,
        unitType.RequestedStartMonth is null ? null : new RequestedPeriodResponse(
            unitType.RequestedStartMonth.Value.ToString("yyyy-MM"),
            unitType.RequestedEndMonth!.Value.ToString("yyyy-MM")),
        unitType.AvailableCapacity, unitType.ImageUrl, unitType.VideoUrl);
}
