namespace Frms.Api.DTOs.Responses;

/// <summary>CON-005: persisted return progress and selected Inspection, never an estimated settlement.</summary>
public sealed record ContractReturnSummaryResponse(Guid ContractId, string ContractStatus,
    VisitDetailResponse? ReturnVisit, ReturnInspectionSummaryResponse? Inspection,
    IReadOnlyList<ReturnDamageDetailResponse> Damages, IReadOnlyList<ReturnFeeDetailResponse> ExtraFees,
    DepositSettlementResponse? Settlement);
/// <summary>Selected unfinished or most recently completed Inspection, without binary evidence.</summary>
public sealed record ReturnInspectionSummaryResponse(Guid InspectionId, string Status,
    string? ConditionNote, DateTimeOffset? CompletedAt);
/// <summary>Recorded Damage decision for the selected Inspection; amount is not recalculated.</summary>
public sealed record ReturnDamageDetailResponse(Guid DamageRecordId, Guid InspectionId, Guid DamageTypeId,
    decimal DamageAmount, string? Note, string Status, DateTimeOffset CreatedAt);
/// <summary>Recorded ExtraFee for the selected Inspection.</summary>
public sealed record ReturnFeeDetailResponse(Guid ExtraFeeId, Guid InspectionId, Guid ExtraFeeTypeId,
    decimal Amount, string Reason, DateTimeOffset CreatedAt);
