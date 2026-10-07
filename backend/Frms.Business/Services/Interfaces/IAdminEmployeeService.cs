using Frms.Business.Models.Commands;
using Frms.DataAccess.Persistence.Entities;

namespace Frms.Business.Services.Interfaces;

public interface IAdminEmployeeService {
    Task<(
        UserAccount Account,
        Employee Employee,
        string RoleName)> CreateAsync(
            CreateAdminEmployeeCommand command,
            CancellationToken cancellationToken = default);

    Task<(
        UserAccount Account,
        Employee Employee,
        string RoleName)?> UpdateAsync(
            Guid employeeId,
            UpdateAdminEmployeeCommand command,
            CancellationToken cancellationToken = default);

    Task<(
        UserAccount Account,
        Employee Employee,
        string RoleName)?> ActivateAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default);

    Task<(
        UserAccount Account,
        Employee Employee,
        string RoleName)?> DeactivateAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default);

    Task<(
        UserAccount Account,
        Employee Employee,
        string RoleName)?> ResendInitialCredentialAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default);

    Task<(
        UserAccount Account,
        Employee Employee,
        string RoleName)?> AssignAsync(
            Guid employeeId,
            string role,
            Guid? facilityId,
            CancellationToken cancellationToken = default);
}
