using Frms.DataAccess.Repositories.Models;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IFacilityOperationsReportRepository
{
    Task<bool> FacilityExistsAsync(Guid facilityId, CancellationToken cancellationToken);
    Task<FacilityOperationsRecord> GetOperationsAsync(Guid facilityId, CancellationToken cancellationToken);
}
