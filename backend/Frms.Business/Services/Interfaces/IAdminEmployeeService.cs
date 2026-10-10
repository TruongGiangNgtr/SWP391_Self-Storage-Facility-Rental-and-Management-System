using Frms.Business.Models.Commands;
using Frms.Business.Models.Results;

namespace Frms.Business.Services.Interfaces;

public interface IAdminEmployeeService {
    Task<(
        AccountProfileResult Account,
        EmployeeProfileResult Employee,
        string RoleName)> CreateAsync(
            CreateAdminEmployeeCommand command,
            CancellationToken cancellationToken = default);

    Task<(
        AccountProfileResult Account,
        EmployeeProfileResult Employee,
        string RoleName)?> UpdateAsync(
            Guid employeeId,
            UpdateAdminEmployeeCommand command,
            CancellationToken cancellationToken = default);

    Task<(
        AccountProfileResult Account,
        EmployeeProfileResult Employee,
        string RoleName)?> ActivateAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default);

    Task<(
        AccountProfileResult Account,
        EmployeeProfileResult Employee,
        string RoleName)?> DeactivateAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default);

    Task<(
        AccountProfileResult Account,
        EmployeeProfileResult Employee,
        string RoleName)?> ResendInitialCredentialAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default);

    Task<(
        AccountProfileResult Account,
        EmployeeProfileResult Employee,
        string RoleName)?> AssignAsync(
            Guid employeeId,
            string role,
            Guid? facilityId,
            CancellationToken cancellationToken = default);
}
