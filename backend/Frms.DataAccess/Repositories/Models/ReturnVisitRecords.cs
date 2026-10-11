namespace Frms.DataAccess.Repositories.Models;

public sealed record ReturnContractRecord(Guid ContractId, Guid CustomerId, Guid FacilityId, string Status);
public sealed record ReturnVisitRecord(Guid VisitId, Guid EntityId, Guid? EmployeeId, string VisitType,
    DateOnly VisitDate, DateTime? ActualReturnDate, string Status);
public sealed record ReturnInspectionRecord(Guid InspectionId, string Status, string? ConditionNote, DateTime? CompletedAt);
public sealed record ReturnDamageRecord(Guid DamageRecordId, Guid InspectionId, Guid DamageTypeId,
    decimal DamageAmount, string? Note, string Status, DateTime CreatedAt);
public sealed record ReturnFeeRecord(Guid ExtraFeeId, Guid InspectionId, Guid ExtraFeeTypeId,
    decimal Amount, string Reason, DateTime CreatedAt);
public sealed record SavedReturnSettlementRecord(Guid DepositSettlementId, decimal TotalDeduction,
    decimal RefundAmount, decimal AdditionalAmountDue, string Status);
public sealed record ContractReturnRecord(Guid ContractId, string ContractStatus, ReturnVisitRecord? ReturnVisit,
    ReturnInspectionRecord? Inspection, IReadOnlyList<ReturnDamageRecord> Damages,
    IReadOnlyList<ReturnFeeRecord> ExtraFees, SavedReturnSettlementRecord? Settlement);
