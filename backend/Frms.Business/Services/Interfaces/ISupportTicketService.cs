using Frms.DataAccess.Persistence.Entities;
namespace Frms.Business.Services.Interfaces;
public interface ISupportTicketService
{
    Task<(IReadOnlyList<SupportTicket> Items, int Total)> ListAccessibleAsync(int page, int pageSize, CancellationToken ct);
    Task<SupportTicket> GetAccessibleAsync(Guid ticketId, CancellationToken ct);
    Task<SupportTicket> AssignAsync(Guid ticketId, Guid staffEmployeeId, CancellationToken ct);
    Task<SupportTicket> CancelAsync(Guid ticketId, string reason, CancellationToken ct);
}
