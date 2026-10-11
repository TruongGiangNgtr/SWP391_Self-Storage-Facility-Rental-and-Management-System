namespace Frms.Business.Models.Commands;

public sealed record CreateSupportTicketCommand(Guid ContractId, string Category, string Description);
public sealed record CompleteSupportTicketCommand(Guid TicketId, string ResultNote);
