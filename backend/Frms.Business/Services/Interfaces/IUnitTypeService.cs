using Frms.DataAccess.Persistence.Entities;

namespace Frms.Business.Services.Interfaces;

public interface IUnitTypeService {
    Task<(IReadOnlyList<UnitType> Items, int TotalItems)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<UnitType?> UpdatePriceAsync(
        Guid unitTypeId,
        decimal rentalPrice,
        CancellationToken cancellationToken = default);
}
