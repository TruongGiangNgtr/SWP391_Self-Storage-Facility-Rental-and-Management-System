namespace Frms.DataAccess.Repositories.Models;

public sealed record InspectionSummaryRecord(
    Guid InspectionId, Guid ContractId, Guid StorageUnitId,
    Guid? VisitId, Guid? EmployeeId, Guid FacilityId,
    string Status, string? ConditionNote, DateTime? CompletedAt);

public sealed record InspectionDamageRecord(
    Guid DamageRecordId, Guid DamageTypeId, string DamageTypeName,
    decimal DamageAmount, string? Note, string Status, DateTime CreatedAt);

public sealed record InspectionExtraFeeRecord(
    Guid ExtraFeeId, Guid ExtraFeeTypeId, decimal Amount,
    string Reason, DateTime CreatedAt);

public sealed record InspectionEvidenceRecord(
    Guid InspectionEvidenceId, string EvidenceType, DateTime CreatedAt);

public sealed record InspectionMonitoringDetailRecord(
    InspectionSummaryRecord Inspection,
    IReadOnlyList<InspectionDamageRecord> Damages,
    IReadOnlyList<InspectionExtraFeeRecord> ExtraFees,
    IReadOnlyList<InspectionEvidenceRecord> Evidence);

public sealed record DamageTypeRecord(
    Guid DamageTypeId, string Name, decimal? DefaultAmount, string Status);
