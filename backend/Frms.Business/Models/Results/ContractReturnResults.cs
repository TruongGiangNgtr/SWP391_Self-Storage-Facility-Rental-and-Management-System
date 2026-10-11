namespace Frms.Business.Models.Results;

public sealed record ReturnInspectionSummaryResult(Guid InspectionId, string Status, string? ConditionNote, DateTimeOffset? CompletedAt);
public sealed record ReturnDamageDetailResult(Guid DamageRecordId, Guid InspectionId, Guid DamageTypeId,
    decimal DamageAmount, string? Note, string Status, DateTimeOffset CreatedAt);
public sealed record ReturnFeeDetailResult(Guid ExtraFeeId, Guid InspectionId, Guid ExtraFeeTypeId,
    decimal Amount, string Reason, DateTimeOffset CreatedAt);
public sealed record ContractReturnSummaryResult(Guid ContractId, string ContractStatus, VisitResult? ReturnVisit,
    ReturnInspectionSummaryResult? Inspection, IReadOnlyList<ReturnDamageDetailResult> Damages,
    IReadOnlyList<ReturnFeeDetailResult> ExtraFees, SettlementResult? Settlement);
