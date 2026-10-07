using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Models;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IRenewalRepository
{
    Task<Guid?> GetCustomerIdByUserAccountIdAsync(
        Guid userAccountId,
        CancellationToken cancellationToken);

    Task<Contract?> GetOwnedContractAsync(
        Guid customerId,
        Guid contractId,
        CancellationToken cancellationToken);

    Task<RenewedContractRecord> RenewAsync(
        Guid customerId,
        Guid contractId,
        DateOnly newEndMonth,
        CancellationToken cancellationToken);
}