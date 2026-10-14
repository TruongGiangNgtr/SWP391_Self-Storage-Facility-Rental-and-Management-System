using Frms.DataAccess.Persistence;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class EmployeeDirectoryRepository(FrmsDbContext db) : IEmployeeDirectoryRepository
{
    public async Task<(IReadOnlyList<EmployeeDirectoryRecord> Items, int Total)> ListAsync(
        Guid? facilityId, string? requiredRole, bool activeOnly, int page, int pageSize, CancellationToken ct)
    {
        var query = from e in db.Employees.AsNoTracking()
                    join a in db.UserAccounts.AsNoTracking() on e.UserAccountId equals a.UserAccountId
                    join role in db.UserRoles.AsNoTracking() on a.RoleId equals role.RoleId
                    select new { e, a, role };
        if (facilityId.HasValue) query = query.Where(x => x.e.FacilityId == facilityId.Value);
        if (requiredRole is not null) query = query.Where(x => x.role.RoleName == requiredRole);
        if (activeOnly) query = query.Where(x => x.a.Status == "ACTIVE");
        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(x => x.e.FullName).ThenBy(x => x.e.EmployeeId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new EmployeeDirectoryRecord(x.e.EmployeeId, x.e.FacilityId,
                x.e.FullName, x.role.RoleName, x.a.Status)).ToArrayAsync(ct);
        return (rows, total);
    }

    public Task<EmployeeDirectoryRecord?> GetAsync(Guid employeeId, CancellationToken ct) =>
        (from e in db.Employees.AsNoTracking()
         join a in db.UserAccounts.AsNoTracking() on e.UserAccountId equals a.UserAccountId
         join role in db.UserRoles.AsNoTracking() on a.RoleId equals role.RoleId
         where e.EmployeeId == employeeId
         select new EmployeeDirectoryRecord(e.EmployeeId, e.FacilityId, e.FullName,
             role.RoleName, a.Status)).SingleOrDefaultAsync(ct);
}
