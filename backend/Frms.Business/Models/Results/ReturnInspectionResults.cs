namespace Frms.Business.Models.Results;

public sealed record ConfirmedReturnResult(
    Guid VisitId, DateOnly ActualReturnDate, Guid InspectionId,
    string InspectionStatus, string StorageUnitStatus, string ReturnClassification);

public sealed record SettlementResult(Guid DepositSettlementId, decimal TotalDeduction,
    decimal RefundAmount, decimal AdditionalAmountDue, string Status);

public sealed record FinalizedReturnResult(Guid ContractId, string ContractStatus,
    string StorageUnitStatus, SettlementResult Settlement);

public sealed record InspectionResult(Guid InspectionId, Guid ContractId, Guid StorageUnitId,
    Guid? VisitId, Guid? EmployeeId, string Status, string? ConditionNote, DateTime? CompletedAt);
public sealed record InspectionDamageResult(Guid DamageRecordId, Guid DamageTypeId,
    decimal DamageAmount, string? Note, string Status);
public sealed record InspectionFeeResult(Guid ExtraFeeId, Guid ExtraFeeTypeId,
    decimal Amount, string Reason);
public sealed record InspectionEvidenceResult(Guid InspectionEvidenceId,
    string EvidenceType, DateTime CreatedAt);
public sealed record InspectionDetailsResult(InspectionResult Inspection,
    IReadOnlyList<InspectionDamageResult> Damages,
    IReadOnlyList<InspectionFeeResult> ExtraFees,
    IReadOnlyList<InspectionEvidenceResult> Evidence);
public sealed record InspectionPageResult(IReadOnlyList<InspectionResult> Items,
    int Page, int PageSize, int TotalItems);
public sealed record DamageTypeResult(Guid DamageTypeId, string Name,
    decimal? DefaultAmount, string Status);
