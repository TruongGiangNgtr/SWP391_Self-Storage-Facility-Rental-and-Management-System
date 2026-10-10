using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Interfaces;

public interface IInspectionService
{
    Task<DamageDecisionRecord> DecideDamageAsync(
        Guid damageRecordId, string decision,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<InspectionSummaryRecord> Items, int TotalItems)> ListAccessibleAsync(
        int page, int pageSize, CancellationToken cancellationToken = default);
    Task<InspectionMonitoringDetailRecord> GetAccessibleAsync(
        Guid inspectionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DamageTypeRecord>> ListActiveDamageTypesAsync(
        CancellationToken cancellationToken = default);
}
