using System.Security.Cryptography;
using Frms.Business.Abstractions.External;
using Frms.Business.Abstractions.Security;
using Frms.Business.Exceptions;
using Frms.Business.Models.Commands;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;

namespace Frms.Business.Services.Implementations;

internal sealed class AdminEmployeeService(
    IAdminEmployeeRepository adminEmployeeRepository,
    IPasswordHasher passwordHasher,
    IEmailService emailService) : IAdminEmployeeService {
    private const string FacilityStaff = "FACILITY_STAFF";
    private const string FacilityManager = "FACILITY_MANAGER";
    private const string BusinessOperationsManager =
        "BUSINESS_OPERATIONS_MANAGER";
    private const string SystemAdministrator =
        "SYSTEM_ADMINISTRATOR";

    public async Task<(
        UserAccount Account,
        Employee Employee,
        string RoleName)> CreateAsync(
            CreateAdminEmployeeCommand command,
            CancellationToken cancellationToken = default) {
        var fullName = command.FullName.Trim();
        var email = command.Email.Trim().ToLowerInvariant();
        var phoneNumber = command.PhoneNumber.Trim();
        var role = command.Role.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(fullName))
            throw Validation("fullName", "Full name is required.");

        if (string.IsNullOrWhiteSpace(email))
            throw Validation("email", "Email is required.");

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw Validation(
                "phoneNumber",
                "Phone number is required.");

        ValidateEmployeeRole(role);

        await ValidateFacilityAssignmentAsync(
            role,
            command.FacilityId,
            cancellationToken);

        if (await adminEmployeeRepository.EmailExistsAsync(
                email,
                cancellationToken)) {
            throw new AdminEmployeeOperationException(
                "EMAIL_ALREADY_EXISTS",
                "Email already exists.",
                "email");
        }

        if (await adminEmployeeRepository.PhoneNumberExistsAsync(
                phoneNumber,
                cancellationToken)) {
            throw new AdminEmployeeOperationException(
                "PHONE_NUMBER_ALREADY_EXISTS",
                "Phone number already exists.",
                "phoneNumber");
        }

        var roleId = await adminEmployeeRepository.GetRoleIdAsync(
            role,
            cancellationToken);

        if (roleId is null) {
            throw Validation(
                "role",
                "Employee role does not exist.");
        }

        var initialPassword = GenerateInitialPassword();
        var passwordHash = passwordHasher.Hash(initialPassword);

        var created = await adminEmployeeRepository.CreateAsync(
            roleId.Value,
            fullName,
            email,
            phoneNumber,
            passwordHash,
            command.FacilityId,
            cancellationToken);

        try {
            await emailService.SendInitialCredentialAsync(
                email,
                initialPassword,
                cancellationToken);
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch {
            // Account intentionally remains INACTIVE.
            // Do not expose/log the plaintext credential.
            throw new AdminEmployeeOperationException(
                "EXTERNAL_PROVIDER_UNAVAILABLE",
                "Initial credential email could not be delivered.");
        }

        await adminEmployeeRepository.SetAccountStatusAsync(
            created.Account.UserAccountId,
            "ACTIVE",
            cancellationToken);

        return (await adminEmployeeRepository.GetByEmployeeIdAsync(
            created.Employee.EmployeeId,
            cancellationToken))!.Value;
    }

    public async Task<(
        UserAccount Account,
        Employee Employee,
        string RoleName)?> UpdateAsync(
            Guid employeeId,
            UpdateAdminEmployeeCommand command,
            CancellationToken cancellationToken = default) {
        var row = await adminEmployeeRepository.GetByEmployeeIdAsync(
            employeeId,
            cancellationToken);

        if (row is null)
            return null;

        var fullName = command.FullName.Trim();

        if (string.IsNullOrWhiteSpace(fullName))
            throw Validation("fullName", "Full name is required.");

        row.Value.Employee.FullName = fullName;

        await adminEmployeeRepository.SaveChangesAsync(
            cancellationToken);

        return row;
    }

    public Task<(
        UserAccount Account,
        Employee Employee,
        string RoleName)?> ActivateAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default)
        => SetStatusAsync(
            employeeId,
            "ACTIVE",
            cancellationToken);

    public Task<(
        UserAccount Account,
        Employee Employee,
        string RoleName)?> DeactivateAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default)
        => SetStatusAsync(
            employeeId,
            "INACTIVE",
            cancellationToken);

    public async Task<(
        UserAccount Account,
        Employee Employee,
        string RoleName)?> ResendInitialCredentialAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default) {
        var row = await adminEmployeeRepository.GetByEmployeeIdAsync(
            employeeId,
            cancellationToken);

        if (row is null)
            return null;

        if (row.Value.Account.Status != "INACTIVE") {
            throw new AdminEmployeeOperationException(
                "CREDENTIAL_PROVISIONING_INVALID_STATUS",
                "Initial credential can only be resent for an INACTIVE employee account.");
        }

        var initialPassword = GenerateInitialPassword();

        row.Value.Account.PasswordHash =
            passwordHasher.Hash(initialPassword);

        // Persist new hash first so the previous unsent credential
        // becomes invalid before retry delivery.
        await adminEmployeeRepository.SaveChangesAsync(
            cancellationToken);

        try {
            await emailService.SendInitialCredentialAsync(
                row.Value.Account.Email,
                initialPassword,
                cancellationToken);
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch {
            // New hash remains persisted, account remains INACTIVE.
            throw new AdminEmployeeOperationException(
                "EXTERNAL_PROVIDER_UNAVAILABLE",
                "Initial credential email could not be delivered.");
        }

        await adminEmployeeRepository.SetAccountStatusAsync(
            row.Value.Account.UserAccountId,
            "ACTIVE",
            cancellationToken);

        return await adminEmployeeRepository.GetByEmployeeIdAsync(
            employeeId,
            cancellationToken);
    }

    private async Task<(
        UserAccount Account,
        Employee Employee,
        string RoleName)?> SetStatusAsync(
            Guid employeeId,
            string status,
            CancellationToken cancellationToken) {
        var row = await adminEmployeeRepository.GetByEmployeeIdAsync(
            employeeId,
            cancellationToken);

        if (row is null)
            return null;

        await adminEmployeeRepository.SetAccountStatusAsync(
            row.Value.Account.UserAccountId,
            status,
            cancellationToken);

        return await adminEmployeeRepository.GetByEmployeeIdAsync(
            employeeId,
            cancellationToken);
    }

    private async Task ValidateFacilityAssignmentAsync(
        string role,
        Guid? facilityId,
        CancellationToken cancellationToken) {
        var facilityScoped =
            role is FacilityStaff or FacilityManager;

        if (facilityScoped && facilityId is null) {
            throw Validation(
                "facilityId",
                "Facility is required for Facility Staff and Facility Manager.");
        }

        if (!facilityScoped && facilityId is not null) {
            throw Validation(
                "facilityId",
                "Global employee roles must not have a Facility assignment.");
        }

        if (facilityId is not null &&
            !await adminEmployeeRepository.FacilityExistsAsync(
                facilityId.Value,
                cancellationToken)) {
            throw new AdminEmployeeOperationException(
                "RESOURCE_NOT_FOUND",
                "Facility was not found.",
                "facilityId");
        }
    }

    private static void ValidateEmployeeRole(string role) {
        if (role is FacilityStaff
            or FacilityManager
            or BusinessOperationsManager
            or SystemAdministrator) {
            return;
        }

        throw Validation(
            "role",
            "Role must be one of the four Employee roles.");
    }

    private static string GenerateInitialPassword() {
        // 8 random bytes -> exactly 16 hexadecimal characters.
        return Convert.ToHexString(
            RandomNumberGenerator.GetBytes(8));
    }

    private static AdminEmployeeOperationException Validation(
        string field,
        string message)
        => new(
            "VALIDATION_ERROR",
            message,
            field);

    public async Task<(
    UserAccount Account,
    Employee Employee,
    string RoleName)?> AssignAsync(
        Guid employeeId,
        string role,
        Guid? facilityId,
        CancellationToken cancellationToken = default) {
        var row = await adminEmployeeRepository.GetByEmployeeIdAsync(
            employeeId,
            cancellationToken);

        if (row is null)
            return null;

        if (string.IsNullOrWhiteSpace(role)) {
            throw Validation(
                "role",
                "Role is required.");
        }

        var normalizedRole = role
            .Trim()
            .ToUpperInvariant();

        ValidateEmployeeRole(normalizedRole);

        await ValidateFacilityAssignmentAsync(
            normalizedRole,
            facilityId,
            cancellationToken);

        await adminEmployeeRepository.UpdateRoleFacilityAsync(
            employeeId,
            normalizedRole,
            facilityId,
            cancellationToken);

        return await adminEmployeeRepository.GetByEmployeeIdAsync(
            employeeId,
            cancellationToken);
    }
}
