using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Interfaces;

public interface IFacilityOperationsReportService
{
    Task<(FacilityOperationsRecord Operations, DateTimeOffset AsOf, decimal UsageRate)> GetAsync(
        Guid facilityId, CancellationToken cancellationToken = default);
}
