namespace Frms.DataAccess.Repositories.Models;

public sealed record InspectionRecord(
    Guid InspectionId, Guid ContractId, Guid StorageUnitId,
    Guid? VisitId, Guid? EmployeeId, string Status,
    string? ConditionNote, DateTime? CompletedAt);

public sealed record DamageRecordItem(
    Guid DamageRecordId, Guid DamageTypeId, decimal DamageAmount,
    string? Note, string Status);

public sealed record ExtraFeeRecordItem(
    Guid ExtraFeeId, Guid ExtraFeeTypeId, decimal Amount, string Reason);

// FileData is deliberately excluded from operational read responses.
public sealed record EvidenceRecordItem(
    Guid InspectionEvidenceId, string EvidenceType, DateTime CreatedAt);

public sealed record InspectionDetailRecord(
    InspectionRecord Inspection,
    IReadOnlyList<DamageRecordItem> Damages,
    IReadOnlyList<ExtraFeeRecordItem> ExtraFees,
    IReadOnlyList<EvidenceRecordItem> Evidence);

public sealed record ActualReturnRecord(
    Guid VisitId, DateOnly ActualReturnDate, Guid InspectionId,
    string InspectionStatus, string StorageUnitStatus,
    string ReturnClassification);

public sealed record ReturnSettlementRecord(
    Guid ContractId, string ContractStatus, string StorageUnitStatus,
    Guid DepositSettlementId, decimal TotalDeduction,
    decimal RefundAmount, decimal AdditionalAmountDue, string Status);
