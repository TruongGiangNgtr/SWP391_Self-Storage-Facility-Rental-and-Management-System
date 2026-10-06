using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Interfaces;

public interface IContractService
{
    Task<ContractPageRecord> ListOwnAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ContractDetailRecord> GetOwnAsync(
        Guid contractId,
        CancellationToken cancellationToken = default);
}