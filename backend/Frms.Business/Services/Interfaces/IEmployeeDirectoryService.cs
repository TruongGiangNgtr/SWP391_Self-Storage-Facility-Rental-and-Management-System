using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Interfaces;

public interface IEmployeeDirectoryService
{
    Task<(IReadOnlyList<EmployeeDirectoryRecord> Items, int Total)> ListBusinessAsync(
        int page, int pageSize, CancellationToken ct);
    Task<EmployeeDirectoryRecord> GetBusinessAsync(Guid employeeId, CancellationToken ct);
    Task<(IReadOnlyList<EmployeeDirectoryRecord> Items, int Total)> ListStaffAsync(
        Guid facilityId, int page, int pageSize, CancellationToken ct);
}
