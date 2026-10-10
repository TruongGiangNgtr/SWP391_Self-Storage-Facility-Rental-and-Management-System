using Frms.Business.Models.Results;

namespace Frms.Business.Services.Interfaces;

public interface IUnitTypeService {
    Task<(IReadOnlyList<UnitTypeResult> Items, int TotalItems)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<UnitTypeResult?> UpdatePriceAsync(
        Guid unitTypeId,
        decimal rentalPrice,
        CancellationToken cancellationToken = default);

    Task<(
    IReadOnlyList<UnitTypeResult> Items,
    int TotalItems,
    string? FacilityStatus)> GetFacilityPagedAsync(
        Guid facilityId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<UnitTypeResult?> GetByIdAsync(
        Guid unitTypeId,
        CancellationToken cancellationToken = default);
}
