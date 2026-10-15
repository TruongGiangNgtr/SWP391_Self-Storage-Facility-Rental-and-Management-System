using Frms.DataAccess.Repositories.Models;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface ICustomerRepository
{
    Task<CustomerProfileRecord?> UpdateOwnProfileAsync(
        Guid userAccountId,
        bool updateFullName, string? fullName,
        bool updateAddress, string? address,
        bool updateCccd, string? cccd,
        CancellationToken ct);
    Task<int> CountByFacilityAsync(Guid facilityId, CancellationToken ct);
    Task<IReadOnlyList<CustomerDirectoryRecord>> ListByFacilityAsync(Guid facilityId, int skip, int take, CancellationToken ct);
    Task<CustomerDirectoryRecord?> GetByFacilityAsync(Guid facilityId, Guid customerId, CancellationToken ct);
    Task<int> CountAllAsync(CancellationToken ct);
    Task<IReadOnlyList<CustomerDirectoryRecord>> ListAllAsync(int skip, int take, CancellationToken ct);
    Task<CustomerDirectoryRecord?> GetByIdAsync(Guid customerId, CancellationToken ct);
}
