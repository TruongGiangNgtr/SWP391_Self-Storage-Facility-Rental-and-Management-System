using Frms.DataAccess.Repositories.Models;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IContractRepository {
    Task<Guid?> GetCustomerIdByUserAccountIdAsync(
        Guid userAccountId,
        CancellationToken cancellationToken);

    Task<int> CountByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ContractSummaryRecord>> ListByCustomerAsync(
        Guid customerId,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<int> CountByFacilityAsync(
        Guid facilityId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ContractSummaryRecord>> ListByFacilityAsync(
        Guid facilityId,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<int> CountAllAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ContractSummaryRecord>> ListAllAsync(
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<ContractDetailRecord?> GetDetailAsync(
        Guid customerId,
        Guid contractId,
        CancellationToken cancellationToken);

    Task<ContractDetailRecord?> GetDetailByIdAsync(
        Guid contractId,
        CancellationToken cancellationToken);
}
