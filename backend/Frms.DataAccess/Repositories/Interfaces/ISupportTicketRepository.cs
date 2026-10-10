using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Models;
namespace Frms.DataAccess.Repositories.Interfaces;
public interface ISupportTicketRepository
{
    Task<Guid?> GetCustomerIdAsync(Guid userAccountId, CancellationToken ct);
    Task<Employee?> GetEmployeeAsync(Guid userAccountId, CancellationToken ct);
    Task<int> CountAsync(Guid? customerId, Guid? facilityId, Guid? assignedEmployeeId, CancellationToken ct);
    Task<IReadOnlyList<SupportTicket>> ListAsync(Guid? customerId, Guid? facilityId, Guid? assignedEmployeeId, int skip, int take, CancellationToken ct);
    Task<SupportTicketRecord?> GetAsync(Guid ticketId, CancellationToken ct);
    Task AssignAsync(Guid ticketId, Guid managerEmployeeId, Guid staffEmployeeId, CancellationToken ct);
    Task CancelAsync(Guid ticketId, Guid customerId, CancellationToken ct);
}
