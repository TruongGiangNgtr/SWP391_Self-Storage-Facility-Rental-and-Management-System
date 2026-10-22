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

[TestFixture, Category("Authentication")]
public sealed class AuthenticationEmployeeLoginMatrixTests
{
    private static readonly Guid AccountId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly string[] Roles = ["FACILITY_STAFF", "FACILITY_MANAGER", "BUSINESS_OPERATIONS_MANAGER", "SYSTEM_ADMINISTRATOR", "CUSTOMER", "UNKNOWN", "facility_staff", "", "FACILITY_STAFF ", "ADMIN"];
    private static readonly string[] Statuses = ["ACTIVE", "INACTIVE", "DISABLED", "PENDING", "active"];

    public static IEnumerable<TestCaseData> Cases()
    {
        foreach (var role in Roles)
        foreach (var status in Statuses)
        foreach (var passwordCorrect in new[] { true, false })
            yield return new TestCaseData(role, status, passwordCorrect,
                status == "ACTIVE" && IsEmployeeRole(role) && passwordCorrect)
                .SetName($"employee_login_{Safe(role)}_{Safe(status)}_{(passwordCorrect ? "correct" : "wrong")}_password");
    }

    [TestCaseSource(nameof(Cases))]
    public async Task LoginEmployee_RoleStatusPasswordMatrixEnforcesAuthenticationRules(string role, string status, bool passwordCorrect, bool expectedSuccess)
    {
        var repository = new FakeRepository(new AuthenticationAccount(AccountId, role, status,
            "staff@example.test", "0900000000", "hash", null, Guid.NewGuid(), Guid.NewGuid(), "Test User"));
        var service = new AuthenticationService(repository, new FakeHasher(passwordCorrect), new FakeTokens(), new FakeClock(), NullLogger<AuthenticationService>.Instance);
        if (expectedSuccess)
        {
            var result = await service.LoginEmployeeAsync(new EmployeeLoginCommand(" STAFF@EXAMPLE.TEST ", passwordCorrect ? "ValidPassword!" : "WrongPassword!", null, null), default);
            Assert.That(result.User.Role, Is.EqualTo(role));
            Assert.That(repository.History, Is.EqualTo(new[] { true }));
        }
        else
        {
            var error = Assert.ThrowsAsync<BusinessException>(() => service.LoginEmployeeAsync(
                new EmployeeLoginCommand(" STAFF@EXAMPLE.TEST ", passwordCorrect ? "ValidPassword!" : "WrongPassword!", null, null), default));
            Assert.That(error!.Code, Is.EqualTo(status == "ACTIVE" ? "AUTH_INVALID_CREDENTIALS" : "ACCOUNT_INACTIVE"));
            Assert.That(repository.History, Is.EqualTo(new[] { false }));
        }
        Assert.That(repository.LastEmail, Is.EqualTo("staff@example.test"));
    }

    private static bool IsEmployeeRole(string role) => role is "FACILITY_STAFF" or "FACILITY_MANAGER" or "BUSINESS_OPERATIONS_MANAGER" or "SYSTEM_ADMINISTRATOR";
    private static string Safe(string value) => string.IsNullOrEmpty(value) ? "empty" : value.Replace(' ', '_');
    private sealed class FakeHasher(bool correct) : IPasswordHasher
    {
        public string Hash(string password) => "hash";
        public bool Verify(string password, string passwordHash) => correct && password == "ValidPassword!" && passwordHash == "hash";
    }
    private sealed class FakeTokens : ITokenService { public IssuedToken Generate(Guid userAccountId, string role) => new("token", new DateTimeOffset(2026, 10, 22, 1, 0, 0, TimeSpan.Zero)); }
    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 10, 22, 0, 0, 0, TimeSpan.Zero);
        public TimeZoneInfo BusinessTimeZone => TimeZoneInfo.Utc;
        public DateTimeOffset ToBusinessTime(DateTimeOffset utcTimestamp) => utcTimestamp;
    }
    private sealed class FakeRepository(AuthenticationAccount? account) : IUserAccountRepository
    {
        public string? LastEmail { get; private set; }
        public List<bool> History { get; } = [];
        public Task<AuthenticationAccount?> FindByPhoneNumberAsync(string normalizedPhoneNumber, CancellationToken cancellationToken) => Task.FromResult(account);
        public Task<AuthenticationAccount?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) { LastEmail = normalizedEmail; return Task.FromResult(account); }
        public Task<AuthenticationAccount?> FindByIdAsync(Guid userAccountId, CancellationToken cancellationToken) => Task.FromResult(userAccountId == AccountId ? account : null);
        public Task AppendLoginHistoryAsync(Guid userAccountId, bool succeeded, string? ipAddress, string? deviceInfo, DateTime loginAtUtc, CancellationToken cancellationToken) { History.Add(succeeded); return Task.CompletedTask; }
    }
}
