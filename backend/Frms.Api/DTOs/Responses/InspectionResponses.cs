namespace Frms.Api.DTOs.Responses;

/// <summary>Represents an active seeded DamageType.</summary>
public sealed record DamageTypeResponse(
    Guid DamageTypeId,
    string Name,
    decimal? DefaultAmount,
    string Status);

/// <summary>Represents the finalized Deposit settlement.</summary>
public sealed record DepositSettlementResponse(
    Guid DepositSettlementId,
    decimal TotalDeduction,
    decimal RefundAmount,
    decimal AdditionalAmountDue,
    string Status);

/// <summary>Represents the return-finalization result.</summary>
public sealed record FinalizeReturnResponse(
    Guid ContractId,
    string ContractStatus,
    DepositSettlementResponse Settlement);
