using Frms.Business.Models.Results;
namespace Frms.Business.Services.Interfaces;
public interface ISupportTicketService
{
    Task<(IReadOnlyList<SupportTicketResult> Items, int Total)> ListAccessibleAsync(int page, int pageSize, CancellationToken ct);
    Task<SupportTicketResult> GetAccessibleAsync(Guid ticketId, CancellationToken ct);
    Task<SupportTicketResult> AssignAsync(Guid ticketId, Guid staffEmployeeId, CancellationToken ct);
    Task<SupportTicketResult> CancelAsync(Guid ticketId, string reason, CancellationToken ct);
}
