using Frms.Business.Abstractions.Security;
using Frms.Business.Abstractions.Time;
using Frms.Business.Exceptions;
using Frms.Business.Models.Commands;
using Frms.Business.Models.Results;
using Frms.Business.Services.Implementations;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Frms.UnitTests;

[TestFixture]
public sealed class AuthenticationServiceTests
{
    private static readonly Guid AccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 0, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task LoginCustomerAsync_ActiveCustomerWithCorrectPassword_ReturnsBearerTokenAndRecordsSuccessfulLogin()
    {
        var account = Account("CUSTOMER", "ACTIVE");
        var repository = new FakeRepository(account);
        var result = await CreateService(repository).LoginCustomerAsync(
            new CustomerLoginCommand(" 0900000000 ", "ValidPassword!", "127.0.0.1", "test-agent"), default);

        Assert.Multiple(() =>
        {
            Assert.That(repository.LastPhoneNumber, Is.EqualTo("0900000000"));
            Assert.That(result.AccessToken, Is.EqualTo("signed-token"));
            Assert.That(result.TokenType, Is.EqualTo("Bearer"));
            Assert.That(result.ExpiresAt, Is.EqualTo(Now.AddHours(1)));
            Assert.That(result.User.UserAccountId, Is.EqualTo(AccountId));
            Assert.That(result.User.Role, Is.EqualTo("CUSTOMER"));
            Assert.That(result.User.Status, Is.EqualTo("ACTIVE"));
            Assert.That(repository.History.Select(item => item.Succeeded), Is.EqualTo(new[] { true }));
            Assert.That(repository.History[0].IpAddress, Is.EqualTo("127.0.0.1"));
            Assert.That(repository.History[0].DeviceInfo, Is.EqualTo("test-agent"));
            Assert.That(repository.History[0].LoginAtUtc, Is.EqualTo(Now.UtcDateTime));
        });
    }

    [Test]
    public async Task LoginEmployeeAsync_ActiveEmployeeWithNormalizedEmail_ReturnsToken()
    {
        var repository = new FakeRepository(Account("FACILITY_STAFF", "ACTIVE"));
        var result = await CreateService(repository).LoginEmployeeAsync(
            new EmployeeLoginCommand(" Staff@Example.Test ", "ValidPassword!", null, null), default);

        Assert.Multiple(() =>
        {
            Assert.That(repository.LastEmail, Is.EqualTo("staff@example.test"));
            Assert.That(result.User.Role, Is.EqualTo("FACILITY_STAFF"));
            Assert.That(repository.History.Select(item => item.Succeeded), Is.EqualTo(new[] { true }));
        });
    }

    [TestCase("FACILITY_STAFF")]
    [TestCase("FACILITY_MANAGER")]
    [TestCase("BUSINESS_OPERATIONS_MANAGER")]
    [TestCase("SYSTEM_ADMINISTRATOR")]
    public async Task LoginEmployeeAsync_AllowedEmployeeRole_ReturnsToken(string role)
    {
        var repository = new FakeRepository(Account(role, "ACTIVE"));
        var result = await CreateService(repository).LoginEmployeeAsync(
            new EmployeeLoginCommand("staff@example.test", "ValidPassword!", null, null), default);

        Assert.That(result.User.Role, Is.EqualTo(role));
    }

    [TestCase("UNKNOWN_ROLE")]
    [TestCase("CUSTOMER")]
    public void LoginEmployeeAsync_NonEmployeeRole_ReturnsInvalidCredentialsAndRecordsFailure(string role)
    {
        var repository = new FakeRepository(Account(role, "ACTIVE"));
        var exception = Assert.ThrowsAsync<BusinessException>(() => CreateService(repository).LoginEmployeeAsync(
            new EmployeeLoginCommand("staff@example.test", "ValidPassword!", null, null), default));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Code, Is.EqualTo("AUTH_INVALID_CREDENTIALS"));
            Assert.That(exception.SuggestedStatusCode, Is.EqualTo(401));
            Assert.That(repository.History.Select(item => item.Succeeded), Is.EqualTo(new[] { false }));
        });
    }

    [Test]
    public void LoginCustomerAsync_ResolvedWrongPassword_AppendsFailedHistoryAndReturnsStableCode()
    {
        var repository = new FakeRepository(Account("CUSTOMER", "ACTIVE"));
        var exception = Assert.ThrowsAsync<BusinessException>(() => CreateService(repository).LoginCustomerAsync(
            new CustomerLoginCommand("0900000000", "WrongPassword!", null, null), default));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Code, Is.EqualTo("AUTH_INVALID_CREDENTIALS"));
            Assert.That(exception.SuggestedStatusCode, Is.EqualTo(401));
            Assert.That(repository.History.Select(item => item.Succeeded), Is.EqualTo(new[] { false }));
        });
    }

    [Test]
    public void LoginCustomerAsync_UnknownPhone_ReturnsInvalidCredentialsWithoutLoginHistory()
    {
        var repository = new FakeRepository(null);
        var exception = Assert.ThrowsAsync<BusinessException>(() => CreateService(repository).LoginCustomerAsync(
            new CustomerLoginCommand("0999999999", "WrongPassword!", null, null), default));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Code, Is.EqualTo("AUTH_INVALID_CREDENTIALS"));
            Assert.That(exception.SuggestedStatusCode, Is.EqualTo(401));
            Assert.That(repository.History, Is.Empty);
        });
    }

    [Test]
    public void LoginEmployeeAsync_UnknownEmail_ReturnsInvalidCredentialsWithoutLoginHistory()
    {
        var repository = new FakeRepository(Account("FACILITY_STAFF", "ACTIVE"));
        var exception = Assert.ThrowsAsync<BusinessException>(() => CreateService(repository).LoginEmployeeAsync(
            new EmployeeLoginCommand("missing@example.test", "ValidPassword!", null, null), default));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Code, Is.EqualTo("AUTH_INVALID_CREDENTIALS"));
            Assert.That(exception.SuggestedStatusCode, Is.EqualTo(401));
            Assert.That(repository.History, Is.Empty);
        });
    }

    [Test]
    public void LoginCustomerAsync_AccountWithEmployeeRole_IsRejectedAndRecordedAsFailure()
    {
        var repository = new FakeRepository(Account("FACILITY_STAFF", "ACTIVE"));
        var exception = Assert.ThrowsAsync<BusinessException>(() => CreateService(repository).LoginCustomerAsync(
            new CustomerLoginCommand("0900000000", "ValidPassword!", null, null), default));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Code, Is.EqualTo("AUTH_INVALID_CREDENTIALS"));
            Assert.That(repository.History.Select(item => item.Succeeded), Is.EqualTo(new[] { false }));
        });
    }

    [Test]
    public void LoginCustomerAsync_InactiveResolvedAccount_ReturnsAccountInactiveAndRecordsFailure()
    {
        var repository = new FakeRepository(Account("CUSTOMER", "INACTIVE"));
        var exception = Assert.ThrowsAsync<BusinessException>(() => CreateService(repository).LoginCustomerAsync(
            new CustomerLoginCommand("0900000000", "ValidPassword!", null, null), default));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Code, Is.EqualTo("ACCOUNT_INACTIVE"));
            Assert.That(exception.SuggestedStatusCode, Is.EqualTo(403));
            Assert.That(repository.History.Select(item => item.Succeeded), Is.EqualTo(new[] { false }));
            Assert.That(repository.History[0].UserAccountId, Is.EqualTo(AccountId));
        });
    }

    [Test]
    public async Task GetCurrentAccountAsync_ActiveAccount_ReturnsAccountProfileFields()
    {
        var account = Account("CUSTOMER", "ACTIVE");
        var result = await CreateService(new FakeRepository(account)).GetCurrentAccountAsync(AccountId, default);

        Assert.Multiple(() =>
        {
            Assert.That(result.UserAccountId, Is.EqualTo(account.UserAccountId));
            Assert.That(result.Role, Is.EqualTo(account.Role));
            Assert.That(result.Status, Is.EqualTo(account.Status));
            Assert.That(result.Email, Is.EqualTo(account.Email));
            Assert.That(result.PhoneNumber, Is.EqualTo(account.PhoneNumber));
            Assert.That(result.CustomerId, Is.EqualTo(account.CustomerId));
            Assert.That(result.EmployeeId, Is.EqualTo(account.EmployeeId));
            Assert.That(result.FacilityId, Is.EqualTo(account.FacilityId));
            Assert.That(result.FullName, Is.EqualTo(account.FullName));
        });
    }

    [Test]
    public async Task GetCurrentAccountAsync_CustomerReturnsSavedCccd()
    {
        var account = Account("CUSTOMER", "ACTIVE") with { Cccd = "012345678901" };
        var result = await CreateService(new FakeRepository(account)).GetCurrentAccountAsync(AccountId, default);
        Assert.That(result.Cccd, Is.EqualTo("012345678901"));
    }

    [Test]
    public async Task GetCurrentAccountAsync_EmployeeCannotInheritCccd()
    {
        var account = Account("FACILITY_STAFF", "ACTIVE") with { Cccd = "should-not-leak" };
        var result = await CreateService(new FakeRepository(account)).GetCurrentAccountAsync(AccountId, default);
        Assert.That(result.Cccd, Is.Null);
    }

    [Test]
    public void GetCurrentAccountAsync_InactiveAccount_ThrowsAccountInactive()
    {
        var exception = Assert.ThrowsAsync<BusinessException>(() => CreateService(new FakeRepository(Account("CUSTOMER", "INACTIVE")))
            .GetCurrentAccountAsync(AccountId, default));
        Assert.Multiple(() =>
        {
            Assert.That(exception!.Code, Is.EqualTo("ACCOUNT_INACTIVE"));
            Assert.That(exception.SuggestedStatusCode, Is.EqualTo(403));
        });
    }

    [Test]
    public void GetCurrentAccountAsync_MissingAccount_ThrowsUnauthorized()
    {
        var exception = Assert.ThrowsAsync<BusinessException>(() => CreateService(new FakeRepository(null))
            .GetCurrentAccountAsync(AccountId, default));
        Assert.Multiple(() =>
        {
            Assert.That(exception!.Code, Is.EqualTo("UNAUTHORIZED"));
            Assert.That(exception.SuggestedStatusCode, Is.EqualTo(401));
        });
    }

    [TestCase("CUSTOMER", "CUSTOMER", "ACTIVE", true)]
    [TestCase("CUSTOMER", "CUSTOMER", "INACTIVE", false)]
    [TestCase("FACILITY_MANAGER", "FACILITY_STAFF", "ACTIVE", false)]
    [TestCase("FACILITY_MANAGER", "FACILITY_MANAGER", "ACTIVE", true)]
    public async Task IsActiveAsync_RequiresActiveAccountAndExactRole(string accountRole, string requestedRole, string status, bool expected)
    {
        var result = await CreateService(new FakeRepository(Account(accountRole, status)))
            .IsActiveAsync(AccountId, requestedRole, default);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public async Task IsActiveAsync_MissingAccount_ReturnsFalse() =>
        Assert.That(await CreateService(new FakeRepository(null)).IsActiveAsync(AccountId, "CUSTOMER", default), Is.False);

    private static AuthenticationService CreateService(FakeRepository repository) =>
        new(repository, new FakeHasher(), new FakeTokenService(), new FakeClock(), NullLogger<AuthenticationService>.Instance);

    private static AuthenticationAccount Account(string role, string status) =>
        new(AccountId, role, status, "staff@example.test", "0900000000", "hash", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Test User");

    private sealed class FakeHasher : IPasswordHasher
    {
        public string Hash(string password) => "hash";
        public bool Verify(string password, string passwordHash) => password == "ValidPassword!" && passwordHash == "hash";
    }

    private sealed class FakeTokenService : ITokenService
    {
        public IssuedToken Generate(Guid userAccountId, string role) => new("signed-token", Now.AddHours(1));
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
        public TimeZoneInfo BusinessTimeZone => TimeZoneInfo.Utc;
        public DateTimeOffset ToBusinessTime(DateTimeOffset utcTimestamp) => utcTimestamp;
    }

    private sealed class FakeRepository(AuthenticationAccount? account) : IUserAccountRepository
    {
        public List<LoginHistoryEntry> History { get; } = [];
        public string? LastPhoneNumber { get; private set; }
        public string? LastEmail { get; private set; }

        public Task<AuthenticationAccount?> FindByPhoneNumberAsync(string normalizedPhoneNumber, CancellationToken cancellationToken)
        {
            LastPhoneNumber = normalizedPhoneNumber;
            return Task.FromResult(account is not null && account.PhoneNumber == normalizedPhoneNumber ? account : null);
        }

        public Task<AuthenticationAccount?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            LastEmail = normalizedEmail;
            return Task.FromResult(account is not null && string.Equals(account.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase) ? account : null);
        }

        public Task<AuthenticationAccount?> FindByIdAsync(Guid userAccountId, CancellationToken cancellationToken) =>
            Task.FromResult(account is not null && account.UserAccountId == userAccountId ? account : null);

        public Task AppendLoginHistoryAsync(Guid userAccountId, bool succeeded, string? ipAddress, string? deviceInfo, DateTime loginAtUtc, CancellationToken cancellationToken)
        {
            History.Add(new LoginHistoryEntry(userAccountId, succeeded, ipAddress, deviceInfo, loginAtUtc));
            return Task.CompletedTask;
        }
    }

    private sealed record LoginHistoryEntry(Guid UserAccountId, bool Succeeded, string? IpAddress, string? DeviceInfo, DateTime LoginAtUtc);
}
