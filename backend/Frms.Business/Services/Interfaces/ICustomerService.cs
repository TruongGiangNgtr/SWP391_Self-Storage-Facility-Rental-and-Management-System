using Frms.Business.Models.Commands;
using Frms.Business.Models.Results;
using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Interfaces;

public interface ICustomerService
{
    Task<UpdatedCustomerProfileResult> UpdateOwnProfileAsync(
        UpdateCustomerProfileCommand command, CancellationToken ct);
    Task<CustomerDirectoryPageResult> ListFacilityAsync(Guid facilityId, int page, int pageSize, CancellationToken ct);
    Task<CustomerDirectoryRecord> GetFacilityAsync(Guid facilityId, Guid customerId, CancellationToken ct);
    Task<CustomerDirectoryPageResult> ListBusinessAsync(int page, int pageSize, CancellationToken ct);
    Task<CustomerDirectoryRecord> GetBusinessAsync(Guid customerId, CancellationToken ct);
}
