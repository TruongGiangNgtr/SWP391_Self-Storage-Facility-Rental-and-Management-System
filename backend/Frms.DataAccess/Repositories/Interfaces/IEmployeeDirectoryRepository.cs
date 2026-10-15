using Frms.DataAccess.Repositories.Models;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IEmployeeDirectoryRepository
{
    Task<(IReadOnlyList<EmployeeDirectoryRecord> Items, int Total)> ListAsync(
        Guid? facilityId, string? requiredRole, bool activeOnly, int page, int pageSize, CancellationToken ct);
    Task<EmployeeDirectoryRecord?> GetAsync(Guid employeeId, CancellationToken ct);
}
