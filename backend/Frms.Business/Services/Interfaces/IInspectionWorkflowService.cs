using Frms.Business.Models.Results;

namespace Frms.Business.Services.Interfaces;

public interface IInspectionWorkflowService
{
    Task<InspectionPageResult> ListAsync(int page, int pageSize, CancellationToken ct = default);
    Task<InspectionDetailsResult> GetAsync(Guid inspectionId, CancellationToken ct = default);
    Task<InspectionResult> ClaimAsync(Guid inspectionId, CancellationToken ct = default);
    Task<InspectionDamageResult> RecordDamageAsync(Guid inspectionId, Guid damageTypeId,
        decimal amount, string? note, CancellationToken ct = default);
    Task<InspectionFeeResult> RecordExtraFeeAsync(Guid inspectionId, Guid extraFeeTypeId,
        decimal amount, string reason, CancellationToken ct = default);
    Task<InspectionEvidenceResult> AddEvidenceAsync(Guid inspectionId, byte[] fileData,
        string evidenceType, CancellationToken ct = default);
    Task<InspectionResult> CompleteAsync(Guid inspectionId, string conditionNote,
        CancellationToken ct = default);
    Task<IReadOnlyList<DamageTypeResult>> ListDamageTypesAsync(CancellationToken ct = default);
}
