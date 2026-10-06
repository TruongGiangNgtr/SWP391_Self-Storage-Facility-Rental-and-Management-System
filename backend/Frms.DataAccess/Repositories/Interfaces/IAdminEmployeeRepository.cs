using Frms.DataAccess.Persistence.Entities;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IAdminEmployeeRepository {
    Task<bool> EmailExistsAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<bool> PhoneNumberExistsAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default);

    Task<Guid?> GetRoleIdAsync(
        string roleName,
        CancellationToken cancellationToken = default);

    Task<bool> FacilityExistsAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default);

    Task<(
        UserAccount Account,
        Employee Employee,
        string RoleName)?> GetByEmployeeIdAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default);

    Task<(UserAccount Account, Employee Employee)> CreateAsync(
        Guid roleId,
        string fullName,
        string email,
        string phoneNumber,
        string passwordHash,
        Guid? facilityId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);

    Task SetAccountStatusAsync(
        Guid userAccountId,
        string status,
        CancellationToken cancellationToken = default);

    Task UpdateRoleFacilityAsync(
        Guid employeeId,
        string roleName,
        Guid? facilityId,
        CancellationToken cancellationToken = default);
}
