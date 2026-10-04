using Frms.Business.Abstractions.Security;
using Frms.Business.Abstractions.Time;
using Frms.Business.Exceptions;
using Frms.Business.Models.Commands;
using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Microsoft.Extensions.Logging;

namespace Frms.Business.Services.Implementations;

internal sealed class AuthenticationService(
    IUserAccountRepository repository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IClock clock,
    ILogger<AuthenticationService> logger) : IAuthenticationService
{
    private static readonly HashSet<string> EmployeeRoles =
    ["FACILITY_STAFF", "FACILITY_MANAGER", "BUSINESS_OPERATIONS_MANAGER", "SYSTEM_ADMINISTRATOR"];

    public async Task<AuthenticationResult> LoginCustomerAsync(CustomerLoginCommand command, CancellationToken cancellationToken)
    {
        var account = await repository.FindByPhoneNumberAsync(command.PhoneNumber.Trim(), cancellationToken);
        return await AuthenticateAsync(account, command.Password, command.IpAddress, command.DeviceInfo, role => role == "CUSTOMER", cancellationToken);
    }

    public async Task<AuthenticationResult> LoginEmployeeAsync(EmployeeLoginCommand command, CancellationToken cancellationToken)
    {
        var account = await repository.FindByEmailAsync(command.Email.Trim().ToLowerInvariant(), cancellationToken);
        return await AuthenticateAsync(account, command.Password, command.IpAddress, command.DeviceInfo, EmployeeRoles.Contains, cancellationToken);
    }

    public async Task<CurrentAccountResult> GetCurrentAccountAsync(Guid userAccountId, CancellationToken cancellationToken)
    {
        var account = await repository.FindByIdAsync(userAccountId, cancellationToken) ?? throw Unauthorized();
        if (account.Status != "ACTIVE") throw new BusinessException("ACCOUNT_INACTIVE", "The account is inactive.", 403);
        return new CurrentAccountResult(account.UserAccountId, account.Role, account.Status, account.Email, account.PhoneNumber, account.CustomerId, account.EmployeeId, account.FacilityId, account.FullName);
    }

    public async Task<bool> IsActiveAsync(Guid userAccountId, string role, CancellationToken cancellationToken)
    {
        var account = await repository.FindByIdAsync(userAccountId, cancellationToken);
        return account is { Status: "ACTIVE" } && string.Equals(account.Role, role, StringComparison.Ordinal);
    }

    private async Task<AuthenticationResult> AuthenticateAsync(AuthenticationAccount? account, string password, string? ipAddress, string? deviceInfo, Func<string, bool> roleAllowed, CancellationToken cancellationToken)
    {
        if (account is null)
        {
            logger.LogWarning("Authentication failed for an unknown identifier.");
            throw InvalidCredentials();
        }

        if (account.Status != "ACTIVE")
        {
            await RecordAsync(account.UserAccountId, false, ipAddress, deviceInfo, cancellationToken);
            throw new BusinessException("ACCOUNT_INACTIVE", "The account is inactive.", 403);
        }

        if (!roleAllowed(account.Role) || !passwordHasher.Verify(password, account.PasswordHash))
        {
            await RecordAsync(account.UserAccountId, false, ipAddress, deviceInfo, cancellationToken);
            throw InvalidCredentials();
        }

        await RecordAsync(account.UserAccountId, true, ipAddress, deviceInfo, cancellationToken);
        var token = tokenService.Generate(account.UserAccountId, account.Role);
        return new AuthenticationResult(token.AccessToken, "Bearer", token.ExpiresAt, new AuthenticatedUserResult(account.UserAccountId, account.Role, account.Status));
    }

    private Task RecordAsync(Guid accountId, bool succeeded, string? ipAddress, string? deviceInfo, CancellationToken cancellationToken) =>
        repository.AppendLoginHistoryAsync(accountId, succeeded, ipAddress, deviceInfo, clock.UtcNow.UtcDateTime, cancellationToken);

    private static BusinessException InvalidCredentials() => new("AUTH_INVALID_CREDENTIALS", "The supplied credentials are invalid.", 401);
    private static BusinessException Unauthorized() => new("UNAUTHORIZED", "Authentication is required.", 401);
}
