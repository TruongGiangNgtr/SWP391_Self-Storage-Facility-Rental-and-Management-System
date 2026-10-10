using Frms.Business.Abstractions.Security;
using Frms.Business.Abstractions.Time;
using Frms.Business.Exceptions;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Implementations;

internal sealed class FacilityOperationsReportService(
    IFacilityOperationsReportRepository repository,
    IFacilityAuthorizationService facilityAuthorizationService,
    ICurrentUserContext currentUser,
    IClock clock) : IFacilityOperationsReportService
{
    public async Task<(FacilityOperationsRecord Operations, DateTimeOffset AsOf, decimal UsageRate)> GetAsync(
        Guid facilityId, CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated)
            throw new BusinessException("UNAUTHORIZED", "Authentication is required.", 401);
        if (currentUser.Role != "FACILITY_MANAGER")
            throw new BusinessException("FORBIDDEN", "Facility Manager role is required.", 403);
        await facilityAuthorizationService.EnsureSameFacilityAsync(facilityId, cancellationToken);
        if (!await repository.FacilityExistsAsync(facilityId, cancellationToken))
            throw new BusinessException("RESOURCE_NOT_FOUND", "Facility was not found.", 404);

        var asOf = clock.UtcNow;
        var operations = await repository.GetOperationsAsync(facilityId, cancellationToken);
        var rate = operations.TotalUnits == 0 ? 0m :
            Math.Round((decimal)operations.InUseUnits / operations.TotalUnits, 4,
                MidpointRounding.AwayFromZero);
        return (operations, asOf, rate);
    }
}
