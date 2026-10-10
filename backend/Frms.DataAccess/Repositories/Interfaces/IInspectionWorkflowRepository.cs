using Frms.DataAccess.Repositories.Models;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IInspectionWorkflowRepository
{
    Task<Guid?> GetEmployeeIdAsync(Guid userAccountId, Guid facilityId, CancellationToken ct);
    Task<Guid?> GetInspectionFacilityIdAsync(Guid inspectionId, CancellationToken ct);
    Task<(IReadOnlyList<InspectionRecord> Items, int Total)> ListAsync(Guid facilityId, int skip, int take, CancellationToken ct);
    Task<InspectionWorkflowDetailRecord?> GetDetailAsync(Guid inspectionId, Guid facilityId, CancellationToken ct);
    Task<IReadOnlyList<(Guid Id, string Name, decimal? DefaultAmount, string Status)>> GetDamageTypesAsync(CancellationToken ct);
    Task<InspectionRecord> ClaimAsync(Guid inspectionId, Guid staffId, CancellationToken ct);
    Task<DamageRecordItem> RecordDamageAsync(Guid inspectionId, Guid typeId, decimal amount, string? note, Guid staffId, CancellationToken ct);
    Task<ExtraFeeRecordItem> RecordExtraFeeAsync(Guid inspectionId, Guid typeId, decimal amount, string reason, Guid staffId, CancellationToken ct);
    Task<EvidenceRecordItem> AddEvidenceAsync(Guid inspectionId, byte[] data, string type, Guid staffId, CancellationToken ct);
    Task<InspectionRecord?> CompleteAsync(Guid inspectionId, string note, Guid staffId, CancellationToken ct);
}
