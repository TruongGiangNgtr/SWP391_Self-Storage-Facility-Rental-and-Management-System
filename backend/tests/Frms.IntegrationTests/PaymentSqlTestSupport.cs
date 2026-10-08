using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Frms.IntegrationTests;

internal static class PaymentSqlTestSupport
{
    internal static string Prepare(Guid id, decimal amount) => System.Text.Json.JsonSerializer.Serialize(new { id, amount });
    internal const string BaselineMigration = "20261004191200_PhaseZeroBaselineConstraints";
    internal const string PaymentMigration = "20261007045825_PaymentProcessing";
    internal const string VnPayMigration = "20261007170917_VnPayPaymentGateway";
    internal static readonly DateTimeOffset Now = new(2026, 10, 7, 10, 0, 0, TimeSpan.Zero);
    internal static readonly DateTimeOffset ProviderTime = new(2026, 10, 7, 17, 2, 3, 456, TimeSpan.FromHours(7));

    internal static string ConnectionString()
    {
        var configured = Environment.GetEnvironmentVariable("FRMS_TEST_CONNECTION_STRING");
        Assert.That(configured, Is.Not.Null.And.Not.Empty,
            "FRMS_TEST_CONNECTION_STRING must identify an explicitly approved disposable SQL Server database.");
        var builder = new SqlConnectionStringBuilder(configured!);
        Assert.That(builder.InitialCatalog, Does.StartWith("Frms_Test_"));
        return builder.ConnectionString;
    }

    internal static FrmsDbContext Open(string? connection = null, Action<string>? log = null)
    {
        var options = new DbContextOptionsBuilder<FrmsDbContext>().UseSqlServer(connection ?? ConnectionString());
        if (log is not null) options.LogTo(log);
        return new FrmsDbContext(options.Options);
    }

    internal static async Task<Invoice> SeedInvoiceAsync(FrmsDbContext db, string type = "DEPOSIT",
        string status = "UNPAID", decimal amount = 12500m, bool firstMonth = false)
    {
        var accountId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var facilityId = Guid.NewGuid();
        var unitTypeId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        db.AddRange(
            new UserAccount { UserAccountId = accountId, RoleId = SeedIds.CustomerRole,
                Email = $"{accountId:N}@example.invalid", PhoneNumber = accountId.ToString("N")[..25],
                PasswordHash = "unused-payment-fixture", Status = "ACTIVE", CreatedAt = Now.UtcDateTime },
            new Customer { CustomerId = customerId, UserAccountId = accountId, FullName = "Payment fixture" },
            new Facility { FacilityId = facilityId, Name = "Payment fixture", Address = "Test", Status = "ACTIVE" },
            new UnitType { UnitTypeId = unitTypeId, Name = "Payment fixture", Mode = "PUBLIC", Size = "Test", RentalPrice = 12500m },
            new Reservation { ReservationId = reservationId, CustomerId = customerId, FacilityId = facilityId,
                UnitTypeId = unitTypeId, PolicyId = SeedIds.InitialPolicy, StartMonth = new(2026, 10, 1),
                EndMonth = new(2026, 12, 1), LockedRentalPrice = 12500m, DepositAmount = 12500m,
                Status = "PENDING_DEPOSIT", CreatedAt = Now.UtcDateTime });
        var entityId = reservationId;
        if (type == "RENTAL_FEE")
        {
            var unitId = Guid.NewGuid();
            entityId = Guid.NewGuid();
            db.AddRange(new StorageUnit { StorageUnitId = unitId, FacilityId = facilityId, UnitTypeId = unitTypeId,
                    UnitCode = entityId.ToString("N"), Status = "IN_USE" },
                new Contract { ContractId = entityId, ReservationId = reservationId, CustomerId = customerId,
                    FacilityId = facilityId, StorageUnitId = unitId, PolicyId = SeedIds.InitialPolicy,
                    StartMonth = new(2026, 10, 1), EndMonth = new(2026, 12, 1), Status = "ACTIVE" });
        }
        var invoice = new Invoice { InvoiceId = Guid.NewGuid(), EntityId = entityId, InvoiceType = type,
            BillingMonth = type == "DEPOSIT" ? null : new DateOnly(2026, firstMonth ? 10 : 11, 1),
            BaseAmount = amount, AmountDue = amount, DueDate = Now.UtcDateTime, CreatedAt = Now.UtcDateTime, Status = status };
        db.Add(invoice);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return invoice;
    }

    internal static Task<int> InsertPaymentAsync(FrmsDbContext db, Guid id, Guid? invoiceId, Guid? key,
        string? reference = null, string status = "PENDING", decimal amount = 12500m,
        DateTime? paidAt = null, string method = "VNPAY") => db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT dbo.Payment (PaymentId, InvoiceId, IdempotencyKey, Amount, PaymentMethod, TransactionCode, Status, PaidAt, CreatedAt)
            VALUES ({id}, {invoiceId}, {key}, {amount}, {method}, {reference}, {status}, {paidAt}, {Now.UtcDateTime})
            """);
}
