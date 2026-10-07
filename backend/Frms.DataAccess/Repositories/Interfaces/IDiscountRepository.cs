using Frms.DataAccess.Persistence.Entities;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IDiscountRepository {
    Task<Guid?> GetCustomerIdByUserAccountIdAsync(
        Guid userAccountId,
        CancellationToken cancellationToken);

    Task<Employee?> GetEmployeeByUserAccountIdAsync(
        Guid userAccountId,
        CancellationToken cancellationToken);

    Task<bool> CustomerExistsAsync(
        Guid customerId,
        CancellationToken cancellationToken);

    Task<bool> HasFacilityRelationshipAsync(
        Guid customerId,
        Guid facilityId,
        CancellationToken cancellationToken);

    Task<int> CountByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Discount>> ListByCustomerAsync(
        Guid customerId,
        int skip,
        int take,
        CancellationToken cancellationToken);
}
