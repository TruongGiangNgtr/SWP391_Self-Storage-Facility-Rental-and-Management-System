using Frms.Business.Abstractions.Security;
using Frms.Business.Abstractions.Time;
using Frms.Business.Models.Results;
using Frms.Business.Services.Implementations;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Frms.UnitTests;

[TestFixture, Category("Authentication")]
public sealed class AuthenticationActiveMatrixTests
{
    private static readonly Guid AccountId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public static IEnumerable<TestCaseData> ActiveCases()
    {
        var roles = new[] { "CUSTOMER", "FACILITY_STAFF", "FACILITY_MANAGER", "BUSINESS_OPERATIONS_MANAGER", "SYSTEM_ADMINISTRATOR", "UNKNOWN", "customer", "", "CUSTOMER ", "FACILITY_STAFF_2" };
        var statuses = new[] { "ACTIVE", "INACTIVE", "DISABLED", "PENDING", "active" };
        foreach (var role in roles)
        foreach (var status in statuses)
        {
            yield return new TestCaseData(role, status, role, status == "ACTIVE").SetName($"matched_role_{Safe(role)}_status_{Safe(status)}");
            yield return new TestCaseData(role, status, "DIFFERENT_ROLE", false).SetName($"mismatched_role_{Safe(role)}_status_{Safe(status)}");
        }
    }

    [TestCaseSource(nameof(ActiveCases))]
    public async Task IsActiveAsync_ExactRoleAndActiveStatusMatrix(string accountRole, string status, string requestedRole, bool expected)
    {
        var account = new AuthenticationAccount(AccountId, accountRole, status, "user@example.test", "0900000000", "hash", null, Guid.NewGuid(), Guid.NewGuid(), "Test User");
        var repository = new FakeRepository(account);
        var service = new AuthenticationService(repository, new FakeHasher(), new FakeTokens(), new FakeClock(), NullLogger<AuthenticationService>.Instance);
        Assert.That(await service.IsActiveAsync(AccountId, requestedRole, default), Is.EqualTo(expected));
    }

    private static string Safe(string value) => string.IsNullOrEmpty(value) ? "empty" : value.Replace(' ', '_').Replace('-', '_');
    private sealed class FakeHasher : IPasswordHasher { public string Hash(string password) => "hash"; public bool Verify(string password, string passwordHash) => true; }
    private sealed class FakeTokens : ITokenService { public IssuedToken Generate(Guid userAccountId, string role) => new("token", new DateTimeOffset(2026, 10, 21, 0, 0, 0, TimeSpan.Zero)); }
    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 10, 20, 0, 0, 0, TimeSpan.Zero);
        public TimeZoneInfo BusinessTimeZone => TimeZoneInfo.Utc;
        public DateTimeOffset ToBusinessTime(DateTimeOffset utcTimestamp) => utcTimestamp;
    }
    private sealed class FakeRepository(AuthenticationAccount? account) : IUserAccountRepository
    {
        public Task<AuthenticationAccount?> FindByPhoneNumberAsync(string normalizedPhoneNumber, CancellationToken cancellationToken) => Task.FromResult(account);
        public Task<AuthenticationAccount?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) => Task.FromResult(account);
        public Task<AuthenticationAccount?> FindByIdAsync(Guid userAccountId, CancellationToken cancellationToken) => Task.FromResult(userAccountId == AccountId ? account : null);
        public Task AppendLoginHistoryAsync(Guid userAccountId, bool succeeded, string? ipAddress, string? deviceInfo, DateTime loginAtUtc, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
