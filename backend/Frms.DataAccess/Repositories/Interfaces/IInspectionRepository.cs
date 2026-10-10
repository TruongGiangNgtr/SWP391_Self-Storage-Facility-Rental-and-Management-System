using Frms.DataAccess.Repositories.Models;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IInspectionRepository
{
    Task<Guid?> GetDamageFacilityIdAsync(Guid damageRecordId, CancellationToken cancellationToken);
    Task<Guid?> GetEmployeeIdByUserAccountIdAsync(Guid userAccountId, CancellationToken cancellationToken);
    Task<DamageDecisionRecord> DecideDamageAsync(Guid damageRecordId, Guid managerEmployeeId,
        string decision, CancellationToken cancellationToken);
    Task<int> CountByFacilityAsync(Guid facilityId, CancellationToken cancellationToken);
    Task<IReadOnlyList<InspectionSummaryRecord>> ListByFacilityAsync(
        Guid facilityId, int skip, int take, CancellationToken cancellationToken);
    Task<InspectionDetailRecord?> GetDetailAsync(Guid inspectionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DamageTypeRecord>> ListActiveDamageTypesAsync(CancellationToken cancellationToken);
}
