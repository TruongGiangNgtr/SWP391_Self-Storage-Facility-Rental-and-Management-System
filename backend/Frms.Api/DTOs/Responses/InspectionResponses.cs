namespace Frms.Api.DTOs.Responses;

public sealed record DamageTypeResponse(Guid DamageTypeId, string Name,
    decimal? DefaultAmount, string Status);

public sealed record InspectionSummaryResponse(Guid InspectionId, Guid ContractId,
    Guid StorageUnitId, Guid? VisitId, Guid? EmployeeId, string Status,
    string? ConditionNote, DateTime? CompletedAt);

public sealed record InspectionDamageResponse(Guid DamageRecordId, Guid DamageTypeId,
    decimal DamageAmount, string? Note, string Status);
public sealed record InspectionExtraFeeResponse(Guid ExtraFeeId, Guid ExtraFeeTypeId,
    decimal Amount, string Reason);
public sealed record InspectionEvidenceResponse(Guid InspectionEvidenceId,
    string EvidenceType, DateTime CreatedAt);
public sealed record InspectionDetailResponse(Guid InspectionId, Guid ContractId,
    Guid StorageUnitId, Guid? VisitId, Guid? EmployeeId, string Status,
    string? ConditionNote, IReadOnlyList<InspectionDamageResponse> Damages,
    IReadOnlyList<InspectionExtraFeeResponse> ExtraFees,
    IReadOnlyList<InspectionEvidenceResponse> Evidence, DateTime? CompletedAt);

public sealed record DepositSettlementResponse(Guid DepositSettlementId,
    decimal TotalDeduction, decimal RefundAmount, decimal AdditionalAmountDue, string Status);

public sealed record FinalizeReturnResponse(Guid ContractId, string ContractStatus,
    string StorageUnitStatus, DepositSettlementResponse Settlement);
