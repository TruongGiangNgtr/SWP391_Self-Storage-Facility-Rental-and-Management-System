namespace Frms.Api.DTOs.Responses;

/// <summary>Represents the canonical Contract renewal result.</summary>
public sealed record RenewContractResponse(
    Guid ContractId,
    string OldEndMonth,
    string NewEndMonth,
    decimal AppliedMonthlyPrice,
    string Status);
