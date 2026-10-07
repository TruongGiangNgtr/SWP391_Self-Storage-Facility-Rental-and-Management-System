namespace Frms.Business.Models.Commands;

public sealed record RenewContractCommand(
    Guid ContractId,
    string NewEndMonth);