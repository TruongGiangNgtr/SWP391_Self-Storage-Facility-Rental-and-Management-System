namespace Frms.Api.DTOs.Responses;

/// <summary>INS-009 outcome returned by dbo.usp_DecideDamage.</summary>
public sealed record DamageDecisionResponse(Guid DamageRecordId, string Status);

public sealed record InspectionSummaryResponse(
    Guid InspectionId, Guid ContractId, Guid StorageUnitId,
    Guid? VisitId, Guid? EmployeeId, Guid FacilityId,
    string Status, string? ConditionNote, DateTime? CompletedAt);

public sealed record InspectionDamageResponse(
    Guid DamageRecordId, Guid DamageTypeId, string DamageTypeName,
    decimal DamageAmount, string? Note, string Status, DateTime CreatedAt);

public sealed record InspectionExtraFeeResponse(
    Guid ExtraFeeId, Guid ExtraFeeTypeId, decimal Amount, string Reason, DateTime CreatedAt);

public sealed record InspectionEvidenceMetadataResponse(
    Guid InspectionEvidenceId, string EvidenceType, DateTime CreatedAt);

public sealed record InspectionDetailResponse(
    InspectionSummaryResponse Inspection,
    IReadOnlyList<InspectionDamageResponse> Damages,
    IReadOnlyList<InspectionExtraFeeResponse> ExtraFees,
    IReadOnlyList<InspectionEvidenceMetadataResponse> Evidence);
