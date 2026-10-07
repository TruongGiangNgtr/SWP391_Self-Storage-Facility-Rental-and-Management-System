using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Interfaces;

public interface IContractService {
    Task<ContractPageRecord> ListAccessibleAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ContractDetailRecord> GetAccessibleAsync(
        Guid contractId,
        CancellationToken cancellationToken = default);
}
