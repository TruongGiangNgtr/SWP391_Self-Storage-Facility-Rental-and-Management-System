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
public sealed class AuthenticationServiceEdgeCaseTests
{
    private static readonly Guid AccountId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly string[] EmployeeRoles =
    [
        "FACILITY_STAFF", "FACILITY_MANAGER", "BUSINESS_OPERATIONS_MANAGER", "SYSTEM_ADMINISTRATOR"
    ];

    [TestCaseSource(nameof(AllowedEmployeeRoles))]
    public async Task LoginEmployeeAsync_EachSupportedEmployeeRole_IsAccepted(string role)
    {
        var repository = new FakeRepository(Account(role, "ACTIVE"));
        var result = await CreateService(repository).LoginEmployeeAsync(
            new EmployeeLoginCommand("employee@example.test", "CorrectPassword!", null, null), default);
        Assert.Multiple(() =>
        {
            Assert.That(result.User.Role, Is.EqualTo(role));
            Assert.That(repository.History, Is.EqualTo(new[] { true }));
        });
    }

    [TestCaseSource(nameof(RejectedEmployeeRoles))]
    public async Task LoginEmployeeAsync_UnsupportedRole_IsRejectedAndRecordedAsFailure(string role)
    {
        var repository = new FakeRepository(Account(role, "ACTIVE"));
        var error = Assert.ThrowsAsync<BusinessException>(() => CreateService(repository).LoginEmployeeAsync(
            new EmployeeLoginCommand("employee@example.test", "CorrectPassword!", null, null), default));
        Assert.Multiple(() =>
        {
            Assert.That(error!.Code, Is.EqualTo("AUTH_INVALID_CREDENTIALS"));
            Assert.That(error.SuggestedStatusCode, Is.EqualTo(401));
            Assert.That(repository.History, Is.EqualTo(new[] { false }));
        });
    }

    [TestCaseSource(nameof(AccountStatuses))]
    public async Task IsActiveAsync_OnlyExactActiveStatusIsAccepted(string status, bool expected)
    {
        var service = CreateService(new FakeRepository(Account("FACILITY_STAFF", status)));
        Assert.That(await service.IsActiveAsync(AccountId, "FACILITY_STAFF", default), Is.EqualTo(expected));
    }

    [TestCaseSource(nameof(CustomerRoleMismatches))]
    public async Task LoginCustomerAsync_NonCustomerRoleIsRejected(string role)
    {
        var repository = new FakeRepository(Account(role, "ACTIVE"));
        var error = Assert.ThrowsAsync<BusinessException>(() => CreateService(repository).LoginCustomerAsync(
            new CustomerLoginCommand("0900000000", "CorrectPassword!", null, null), default));
        Assert.Multiple(() =>
        {
            Assert.That(error!.Code, Is.EqualTo("AUTH_INVALID_CREDENTIALS"));
            Assert.That(repository.History, Is.EqualTo(new[] { false }));
        });
    }

    [TestCaseSource(nameof(InactiveEmployeeStatuses))]
    public async Task LoginEmployeeAsync_InactiveStatusReturnsAccountInactive(string status)
    {
        var repository = new FakeRepository(Account("FACILITY_MANAGER", status));
        var error = Assert.ThrowsAsync<BusinessException>(() => CreateService(repository).LoginEmployeeAsync(
            new EmployeeLoginCommand("employee@example.test", "CorrectPassword!", null, null), default));
        Assert.Multiple(() =>
        {
            Assert.That(error!.Code, Is.EqualTo("ACCOUNT_INACTIVE"));
            Assert.That(error.SuggestedStatusCode, Is.EqualTo(403));
            Assert.That(repository.History, Is.EqualTo(new[] { false }));
        });
    }

    [Test]
    public async Task IsActiveAsync_MissingAccount_ReturnsFalse()
    {
        var service = CreateService(new FakeRepository(null));
        Assert.That(await service.IsActiveAsync(AccountId, "CUSTOMER", default), Is.False);
    }

    [TestCase("FACILITY_STAFF", "FACILITY_STAFF", true)]
    [TestCase("FACILITY_STAFF", "FACILITY_MANAGER", false)]
    [TestCase("CUSTOMER", "CUSTOMER", true)]
    [TestCase("CUSTOMER", "customer", false)]
    [TestCase("SYSTEM_ADMINISTRATOR", "SYSTEM_ADMINISTRATOR", true)]
    [TestCase("SYSTEM_ADMINISTRATOR", "system_administrator", false)]
    public async Task IsActiveAsync_RoleComparisonIsExact(string accountRole, string requestedRole, bool expected)
    {
        var service = CreateService(new FakeRepository(Account(accountRole, "ACTIVE")));
        Assert.That(await service.IsActiveAsync(AccountId, requestedRole, default), Is.EqualTo(expected));
    }

    public static IEnumerable<TestCaseData> AllowedEmployeeRoles()
    {
        foreach (var role in EmployeeRoles) yield return new TestCaseData(role).SetName($"employee_role_{role}");
    }

    public static IEnumerable<TestCaseData> RejectedEmployeeRoles()
    {
        foreach (var role in new[] { "CUSTOMER", "UNKNOWN_ROLE", "customer", "facility_staff", "", "SYSTEM_ADMIN", "FACILITY_ADMIN" })
            yield return new TestCaseData(role).SetName($"rejected_employee_role_{(string.IsNullOrEmpty(role) ? "empty" : role.Replace(' ', '_'))}");
    }

    public static IEnumerable<TestCaseData> AccountStatuses()
    {
        yield return new TestCaseData("ACTIVE", true).SetName("status_case_0_ACTIVE");
        var index = 1;
        foreach (var status in new[] { "INACTIVE", "DISABLED", "LOCKED", "PENDING", "SUSPENDED", "active", "Active", " ACTIVE ", "", "UNKNOWN" })
        {
            yield return new TestCaseData(status, false).SetName($"status_case_{index}_{(string.IsNullOrEmpty(status) ? "empty" : status.Trim().Replace(' ', '_'))}");
            index++;
        }
    }

    public static IEnumerable<TestCaseData> CustomerRoleMismatches()
    {
        foreach (var role in new[] { "FACILITY_STAFF", "FACILITY_MANAGER", "BUSINESS_OPERATIONS_MANAGER", "SYSTEM_ADMINISTRATOR", "customer" })
            yield return new TestCaseData(role).SetName($"customer_login_rejects_{role}");
    }

    public static IEnumerable<TestCaseData> InactiveEmployeeStatuses()
    {
        foreach (var status in new[] { "INACTIVE", "DISABLED", "LOCKED", "SUSPENDED" })
            yield return new TestCaseData(status).SetName($"employee_inactive_{status}");
    }

    private static AuthenticationService CreateService(FakeRepository repository) =>
        new(repository, new FakeHasher(), new FakeTokenService(), new FakeClock(), NullLogger<AuthenticationService>.Instance);

    private static AuthenticationAccount Account(string role, string status) =>
        new(AccountId, role, status, "employee@example.test", "0900000000", "hash", null, Guid.NewGuid(), Guid.NewGuid(), "Edge Case User");

    private sealed class FakeHasher : IPasswordHasher
    {
        public string Hash(string password) => "hash";
        public bool Verify(string password, string passwordHash) => password == "CorrectPassword!" && passwordHash == "hash";
    }

    private sealed class FakeTokenService : ITokenService
    {
        public IssuedToken Generate(Guid userAccountId, string role) =>
            new("edge-test-token", new DateTimeOffset(2026, 10, 20, 0, 0, 0, TimeSpan.Zero));
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 10, 20, 0, 0, 0, TimeSpan.Zero);
        public TimeZoneInfo BusinessTimeZone => TimeZoneInfo.Utc;
        public DateTimeOffset ToBusinessTime(DateTimeOffset utcTimestamp) => utcTimestamp;
    }

    private sealed class FakeRepository(AuthenticationAccount? account) : IUserAccountRepository
    {
        public List<bool> History { get; } = [];
        public Task<AuthenticationAccount?> FindByPhoneNumberAsync(string normalizedPhoneNumber, CancellationToken cancellationToken) => Task.FromResult(account);
        public Task<AuthenticationAccount?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) => Task.FromResult(account);
        public Task<AuthenticationAccount?> FindByIdAsync(Guid userAccountId, CancellationToken cancellationToken) => Task.FromResult(account);
        public Task AppendLoginHistoryAsync(Guid userAccountId, bool succeeded, string? ipAddress, string? deviceInfo, DateTime loginAtUtc, CancellationToken cancellationToken)
        {
            History.Add(succeeded);
            return Task.CompletedTask;
        }
    }
}
