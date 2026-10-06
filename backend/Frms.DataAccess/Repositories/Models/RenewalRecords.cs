namespace Frms.DataAccess.Repositories.Models;

public sealed record RenewedContractRecord(
    Guid ContractId,
    DateOnly OldEndMonth,
    DateOnly NewEndMonth,
    decimal AppliedMonthlyPrice,
    string Status);