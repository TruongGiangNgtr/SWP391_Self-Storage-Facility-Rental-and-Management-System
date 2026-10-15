using Frms.DataAccess.Repositories.Models;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IAdminCustomerStatusRepository
{
    Task<CustomerAccountStatusRecord?> SetStatusAsync(Guid customerId, string newStatus, CancellationToken ct);
}
