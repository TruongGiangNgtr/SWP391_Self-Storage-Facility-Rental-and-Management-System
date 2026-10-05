using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Frms.Api.DTOs.Responses;
using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Frms.IntegrationTests;

[TestFixture]
[NonParallelizable]
public sealed class RealSqlServerAuthenticationTests
{
    [Test]
    public async Task INT_AUTH_001_CustomerAndEmployeeLogin_UseRealSqlServerBcryptAndJwtAndRejectInactiveAccount()
    {
        var connectionString = Environment.GetEnvironmentVariable("FRMS_TEST_CONNECTION_STRING");
        Assert.That(connectionString, Is.Not.Null.And.Not.Empty, "FRMS_TEST_CONNECTION_STRING is required for this real SQL Server integration test.");
        Assert.That(new SqlConnectionStringBuilder(connectionString!).InitialCatalog, Does.StartWith("Frms_Test_"), "Use a disposable database named Frms_Test_*; fixture data must never target a production database.");
        using var environment = ConfigureRealSqlEnvironment(connectionString!);
        await using var factory = new RealSqlServerFactory();
        var fixture = await SeedAuthenticationAccountsAsync(factory.Services);
        using var client = factory.CreateClient();

        var customerLogin = await client.PostAsJsonAsync("/api/v1/auth/customer/login", new { phoneNumber = fixture.CustomerPhone, password = TestPassword });
        var customerToken = await customerLogin.Content.ReadFromJsonAsync<ApiResponse<AuthTokenDataResponse>>();
        Assert.Multiple(() =>
        {
            Assert.That(customerLogin.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(customerToken?.Data.AccessToken, Is.Not.Empty);
            Assert.That(customerToken?.Data.User.Role, Is.EqualTo("CUSTOMER"));
        });

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken!.Data.AccessToken);
        var currentAccount = await client.GetAsync("/api/v1/auth/me");
        var currentCustomer = await currentAccount.Content.ReadFromJsonAsync<ApiResponse<CurrentAccountDataResponse>>();
        Assert.That(currentAccount.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(currentCustomer?.Data.PhoneNumber, Is.EqualTo(fixture.CustomerPhone));

        client.DefaultRequestHeaders.Authorization = null;
        var employeeLogin = await client.PostAsJsonAsync("/api/v1/auth/employee/login", new { email = fixture.EmployeeEmail, password = TestPassword });
        var employeeToken = await employeeLogin.Content.ReadFromJsonAsync<ApiResponse<AuthTokenDataResponse>>();
        Assert.Multiple(() =>
        {
            Assert.That(employeeLogin.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(employeeToken?.Data.AccessToken, Is.Not.Empty);
            Assert.That(employeeToken?.Data.User.Role, Is.EqualTo("FACILITY_STAFF"));
        });

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken!.Data.AccessToken);
        var employeeCurrent = await client.GetAsync("/api/v1/auth/me");
        var employeeProfile = await employeeCurrent.Content.ReadFromJsonAsync<ApiResponse<CurrentAccountDataResponse>>();
        Assert.Multiple(() =>
        {
            Assert.That(employeeCurrent.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(employeeProfile?.Data.Email, Is.EqualTo(fixture.EmployeeEmail));
            Assert.That(employeeProfile?.Data.Profile?.FacilityId, Is.EqualTo(fixture.FacilityId));
        });
        client.DefaultRequestHeaders.Authorization = null;

        var inactiveLogin = await client.PostAsJsonAsync("/api/v1/auth/customer/login", new { phoneNumber = fixture.InactivePhone, password = TestPassword });
        var inactiveError = await inactiveLogin.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Multiple(() =>
        {
            Assert.That(inactiveLogin.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(inactiveError?.Code, Is.EqualTo("ACCOUNT_INACTIVE"));
            Assert.That(inactiveError?.TraceId, Is.Not.Empty);
        });

        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<FrmsDbContext>();
            await database.UserAccounts.Where(account => account.PhoneNumber == fixture.CustomerPhone)
                .ExecuteUpdateAsync(update => update.SetProperty(account => account.Status, "INACTIVE"));
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken.Data.AccessToken);
        var revokedAccess = await client.GetAsync("/api/v1/auth/me");
        var revokedError = await revokedAccess.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Multiple(() =>
        {
            Assert.That(revokedAccess.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(revokedError?.Code, Is.EqualTo("UNAUTHORIZED"));
            Assert.That(revokedError?.TraceId, Is.Not.Empty);
        });
    }

    private static async Task<AuthenticationFixture> SeedAuthenticationAccountsAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<FrmsDbContext>();
        await database.Database.MigrateAsync();
        var customerAccountId = Guid.NewGuid();
        var employeeAccountId = Guid.NewGuid();
        var inactiveAccountId = Guid.NewGuid();
        var suffix = Guid.NewGuid().ToString("N");
        var phoneSuffix = string.Concat(Enumerable.Range(0, 8).Select(_ => System.Security.Cryptography.RandomNumberGenerator.GetInt32(10)));
        var customerPhone = $"+849{phoneSuffix}";
        var inactivePhone = $"+848{phoneSuffix}";
        var employeeEmail = $"phase0.{suffix}@example.invalid";
        var facilityId = Guid.NewGuid();
        database.Facilities.Add(new Facility { FacilityId = facilityId, Name = "Phase 0 Test Facility", Address = "Disposable integration fixture", Status = "ACTIVE" });
        var hash = BCrypt.Net.BCrypt.HashPassword(TestPassword, 12);
        database.UserAccounts.AddRange(
            new UserAccount { UserAccountId = customerAccountId, RoleId = SeedIds.CustomerRole, Email = $"customer.{suffix}@example.invalid", PhoneNumber = customerPhone, PasswordHash = hash, Status = "ACTIVE" },
            new UserAccount { UserAccountId = employeeAccountId, RoleId = SeedIds.FacilityStaffRole, Email = employeeEmail, PhoneNumber = $"+847{phoneSuffix}", PasswordHash = hash, Status = "ACTIVE", EmailVerifiedAt = DateTime.UtcNow },
            new UserAccount { UserAccountId = inactiveAccountId, RoleId = SeedIds.CustomerRole, Email = $"inactive.{suffix}@example.invalid", PhoneNumber = inactivePhone, PasswordHash = hash, Status = "INACTIVE" });
        database.Customers.AddRange(
            new Customer { CustomerId = Guid.NewGuid(), UserAccountId = customerAccountId, FullName = "Phase 0 Customer" },
            new Customer { CustomerId = Guid.NewGuid(), UserAccountId = inactiveAccountId, FullName = "Phase 0 Inactive Customer" });
        database.Employees.Add(new Employee { EmployeeId = Guid.NewGuid(), UserAccountId = employeeAccountId, FacilityId = facilityId, FullName = "Phase 0 Employee" });
        await database.SaveChangesAsync();
        return new AuthenticationFixture(customerPhone, employeeEmail, inactivePhone, facilityId);
    }

    private const string TestPassword = "Phase0IntegrationPassword!";
    private sealed record AuthenticationFixture(string CustomerPhone, string EmployeeEmail, string InactivePhone, Guid FacilityId);

    private sealed class RealSqlServerFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureLogging(logging => logging.ClearProviders());
        }
    }

    private static IDisposable ConfigureRealSqlEnvironment(string connectionString)
    {
        var previous = new[] { "ASPNETCORE_ENVIRONMENT", "ConnectionStrings__Frms", "Jwt__Issuer", "Jwt__Audience", "Jwt__SigningKey", "Jwt__LifetimeMinutes" }
            .ToDictionary(key => key, Environment.GetEnvironmentVariable);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("ConnectionStrings__Frms", connectionString);
        Environment.SetEnvironmentVariable("Jwt__Issuer", "frms-real-sql-integration");
        Environment.SetEnvironmentVariable("Jwt__Audience", "frms-real-sql-integration");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
        Environment.SetEnvironmentVariable("Jwt__LifetimeMinutes", "60");
        return new EnvironmentRestore(previous);
    }

    private sealed class EnvironmentRestore(Dictionary<string, string?> previous) : IDisposable
    {
        public void Dispose()
        {
            foreach (var (key, value) in previous) Environment.SetEnvironmentVariable(key, value);
        }
    }
}
