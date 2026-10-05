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

    [Test]
    public async Task LoginCustomerAsync_ActiveCustomerWithCorrectPassword_ReturnsTokenAndSuccessHistory()
    {
        var repository = new FakeRepository(Account("CUSTOMER", "ACTIVE"));
        var result = await CreateService(repository).LoginCustomerAsync(new CustomerLoginCommand(" 0900000000 ", "ValidPassword!", "127.0.0.1", "test-agent"), default);
        Assert.Multiple(() => { Assert.That(result.AccessToken, Is.EqualTo("signed-token")); Assert.That(result.TokenType, Is.EqualTo("Bearer")); Assert.That(result.User.Role, Is.EqualTo("CUSTOMER")); Assert.That(repository.History, Is.EqualTo(new[] { true })); });
    }

    [Test]
    public async Task LoginEmployeeAsync_ActiveEmployeeWithNormalizedEmail_ReturnsToken()
    {
        var repository = new FakeRepository(Account("FACILITY_STAFF", "ACTIVE"));
        var result = await CreateService(repository).LoginEmployeeAsync(new EmployeeLoginCommand(" Staff@Example.Test ", "ValidPassword!", null, null), default);
        Assert.Multiple(() => { Assert.That(repository.LastEmail, Is.EqualTo("staff@example.test")); Assert.That(result.User.Role, Is.EqualTo("FACILITY_STAFF")); Assert.That(repository.History, Is.EqualTo(new[] { true })); });
    }

    [TestCase("FACILITY_MANAGER", true)]
    [TestCase("UNKNOWN_ROLE", false)]
    public async Task LoginEmployeeAsync_RolePartition_EnforcesEmployeeRoles(string role, bool succeeds)
    {
        var repository = new FakeRepository(Account(role, "ACTIVE"));
        if (succeeds) Assert.That((await CreateService(repository).LoginEmployeeAsync(new EmployeeLoginCommand("staff@example.test", "ValidPassword!", null, null), default)).User.Role, Is.EqualTo(role));
        else Assert.That(Assert.ThrowsAsync<BusinessException>(() => CreateService(repository).LoginEmployeeAsync(new EmployeeLoginCommand("staff@example.test", "ValidPassword!", null, null), default))!.Code, Is.EqualTo("AUTH_INVALID_CREDENTIALS"));
    }

    [Test]
    public void LoginCustomerAsync_ResolvedWrongPassword_AppendsFailedHistoryAndReturnsStableCode()
    {
        var repository = new FakeRepository(Account("CUSTOMER", "ACTIVE"));
        var exception = Assert.ThrowsAsync<BusinessException>(() => CreateService(repository).LoginCustomerAsync(new CustomerLoginCommand("0900000000", "WrongPassword!", null, null), default));
        Assert.Multiple(() => { Assert.That(exception!.Code, Is.EqualTo("AUTH_INVALID_CREDENTIALS")); Assert.That(exception.SuggestedStatusCode, Is.EqualTo(401)); Assert.That(repository.History, Is.EqualTo(new[] { false })); });
    }

    [Test]
    public void LoginCustomerAsync_UnknownPhone_DoesNotFabricateLoginHistory()
    {
        var repository = new FakeRepository(null);
        var exception = Assert.ThrowsAsync<BusinessException>(() => CreateService(repository).LoginCustomerAsync(new CustomerLoginCommand("0999999999", "WrongPassword!", null, null), default));
        Assert.Multiple(() => { Assert.That(exception!.Code, Is.EqualTo("AUTH_INVALID_CREDENTIALS")); Assert.That(repository.History, Is.Empty); });
    }

    [Test]
    public void LoginCustomerAsync_InactiveResolvedAccount_AppendsFailedHistoryAndReturnsAccountInactive()
    {
        var repository = new FakeRepository(Account("CUSTOMER", "INACTIVE"));
        var exception = Assert.ThrowsAsync<BusinessException>(() => CreateService(repository).LoginCustomerAsync(new CustomerLoginCommand("0900000000", "ValidPassword!", null, null), default));
        Assert.Multiple(() => { Assert.That(exception!.Code, Is.EqualTo("ACCOUNT_INACTIVE")); Assert.That(exception.SuggestedStatusCode, Is.EqualTo(403)); Assert.That(repository.History, Is.EqualTo(new[] { false })); });
    }

    [Test]
    public async Task IsActiveAsync_RoleChanged_ReturnsFalse() => Assert.That(await CreateService(new FakeRepository(Account("FACILITY_MANAGER", "ACTIVE"))).IsActiveAsync(AccountId, "FACILITY_STAFF", default), Is.False);

    private static AuthenticationService CreateService(FakeRepository repository) => new(repository, new FakeHasher(), new FakeTokenService(), new FakeClock(), NullLogger<AuthenticationService>.Instance);
    private static AuthenticationAccount Account(string role, string status) => new(AccountId, role, status, "staff@example.test", "0900000000", "hash", null, Guid.NewGuid(), Guid.NewGuid(), "Test User");
    private sealed class FakeHasher : IPasswordHasher { public string Hash(string password) => "hash"; public bool Verify(string password, string passwordHash) => password == "ValidPassword!" && passwordHash == "hash"; }
    private sealed class FakeTokenService : ITokenService { public IssuedToken Generate(Guid userAccountId, string role) => new("signed-token", new DateTimeOffset(2026, 10, 4, 1, 0, 0, TimeSpan.Zero)); }
    private sealed class FakeClock : IClock { public DateTimeOffset UtcNow => new(2026, 10, 4, 0, 0, 0, TimeSpan.Zero); public TimeZoneInfo BusinessTimeZone => TimeZoneInfo.Utc; public DateTimeOffset ToBusinessTime(DateTimeOffset utcTimestamp) => utcTimestamp; }
    private sealed class FakeRepository(AuthenticationAccount? account) : IUserAccountRepository
    {
        public List<bool> History { get; } = [];
        public string? LastEmail { get; private set; }
        public Task<AuthenticationAccount?> FindByPhoneNumberAsync(string normalizedPhoneNumber, CancellationToken cancellationToken) => Task.FromResult(account);
        public Task<AuthenticationAccount?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) { LastEmail = normalizedEmail; return Task.FromResult(account); }
        public Task<AuthenticationAccount?> FindByIdAsync(Guid userAccountId, CancellationToken cancellationToken) => Task.FromResult(account);
        public Task AppendLoginHistoryAsync(Guid userAccountId, bool succeeded, string? ipAddress, string? deviceInfo, DateTime loginAtUtc, CancellationToken cancellationToken) { History.Add(succeeded); return Task.CompletedTask; }
    }
}
