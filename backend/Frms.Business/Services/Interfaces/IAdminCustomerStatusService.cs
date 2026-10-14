using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Interfaces;

public interface IAdminCustomerStatusService
{
    Task<CustomerAccountStatusRecord> SetStatusAsync(Guid customerId, string status, CancellationToken ct);
}
