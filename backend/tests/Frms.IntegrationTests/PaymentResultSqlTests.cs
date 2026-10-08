using System.Text.Json;
using Frms.DataAccess.Repositories.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using static Frms.IntegrationTests.PaymentSqlTestSupport;

namespace Frms.IntegrationTests;

public sealed partial class PaymentPersistenceTests
{
    [TestCase("UNPAID")]
    [TestCase("OVERDUE")]
    public async Task DBT_PAY_006_SuccessPaysInvoiceAndStoresVerifiedUtcTime(string invoiceStatus)
    {
        var attempt = await CreateAsync(invoiceStatus: invoiceStatus);
        var input = Success(attempt);
        var result = await repository.ApplyResultAsync(input, default);
        var stored = await db.Payments.AsNoTracking().SingleAsync(row => row.PaymentId == attempt.Detail.PaymentId);
        var audits = await AuditsAsync(attempt.Detail.PaymentId);
        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(PaymentApplyOutcome.Applied));
            Assert.That(result.Payment!.Status, Is.EqualTo("SUCCESS"));
            Assert.That(result.Payment.TransactionCode, Is.EqualTo(input.TransactionCode));
            Assert.That(result.Payment.PaidAt, Is.EqualTo(ProviderTime.ToUniversalTime()));
            Assert.That(result.Payment.PaidAt!.Value.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(stored.PaidAt, Is.EqualTo(ProviderTime.UtcDateTime));
            Assert.That(audits, Has.Count.EqualTo(1));
            Assert.That(audits[0].UserAccountId, Is.Null);
            Assert.That(audits[0].Action, Is.EqualTo("PAYMENT_RESULT"));
            Assert.That(audits[0].EntityType, Is.EqualTo("PAYMENT"));
            Assert.That(JsonDocument.Parse(audits[0].OldValue!).RootElement.GetProperty("Status").GetString(), Is.EqualTo("PENDING"));
            Assert.That(JsonDocument.Parse(audits[0].NewValue!).RootElement.GetProperty("Status").GetString(), Is.EqualTo("SUCCESS"));
        });
        Assert.That(await InvoiceStatusAsync(attempt), Is.EqualTo("PAID"));
    }

    [Test]
    public async Task DBT_PAY_007_VerifiedFailureLeavesInvoiceUnchangedAndNullPaidAt()
    {
        var attempt = await CreateAsync();
        var input = new NormalizedPaymentResult(attempt.Detail.PaymentId, PaymentFinalStatus.Failed,
            Guid.NewGuid().ToString("N"), null, null, PaymentResultSource.VerifiedCallback);
        var first = await repository.ApplyResultAsync(input, default);
        var repeated = await repository.ApplyResultAsync(input, default);
        Assert.Multiple(() =>
        {
            Assert.That(first.Outcome, Is.EqualTo(PaymentApplyOutcome.Applied));
            Assert.That(first.Payment!.Status, Is.EqualTo("FAILED"));
            Assert.That(first.Payment.PaidAt, Is.Null);
            Assert.That(first.Payment.TransactionCode, Is.EqualTo(input.TransactionCode));
            Assert.That(repeated.Outcome, Is.EqualTo(PaymentApplyOutcome.Duplicate));
        });
        Assert.That(await InvoiceStatusAsync(attempt), Is.EqualTo("UNPAID"));
        Assert.That(await AuditsAsync(attempt.Detail.PaymentId), Has.Count.EqualTo(1));
        var retry = await repository.CreateOrGetAsync(attempt.Detail.InvoiceId, attempt.IdempotencyKey, Now, default);
        Assert.That(retry.Attempt!.Detail.Status, Is.EqualTo("FAILED"));
        var newAction = await repository.CreateOrGetAsync(attempt.Detail.InvoiceId, Guid.NewGuid(), Now, default);
        Assert.That(newAction.Outcome, Is.EqualTo(PaymentAttemptOutcome.Created));
        Assert.That(newAction.Attempt!.Detail.PaymentId, Is.Not.EqualTo(attempt.Detail.PaymentId));
    }

    [TestCase("126.50")]
    [TestCase("12500.0001")]
    [TestCase("-125.50")]
    public async Task DBT_PAY_008_MismatchDoesNotMutateMoneyAndDeduplicatesSafeDiagnostic(string amount)
    {
        var attempt = await CreateAsync();
        var input = Success(attempt) with { VerifiedAmount = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture) };
        var first = await repository.ApplyResultAsync(input, default);
        var repeated = await repository.ApplyResultAsync(input, default);
        Assert.Multiple(() =>
        {
            Assert.That(first.Outcome, Is.EqualTo(PaymentApplyOutcome.AmountMismatch));
            Assert.That(repeated.Outcome, Is.EqualTo(PaymentApplyOutcome.AmountMismatch));
            Assert.That(first.Payment!.Status, Is.EqualTo("PENDING"));
            Assert.That(first.Payment.PaidAt, Is.Null);
            Assert.That(first.Payment.TransactionCode, Is.Null);
            Assert.That(first.Payment.Amount, Is.EqualTo(attempt.Detail.Amount));
        });
        Assert.That(await InvoiceStatusAsync(attempt), Is.EqualTo("UNPAID"));
        var audits = await AuditsAsync(attempt.Detail.PaymentId);
        Assert.That(audits, Has.Count.EqualTo(1));
        Assert.That(audits[0].NewValue, Does.Contain("PAYMENT_AMOUNT_MISMATCH"));
    }

    [Test]
    public async Task DBT_PAY_009_ExactDuplicateIsNoOpIncludingAudit()
    {
        var attempt = await CreateAsync();
        var input = Success(attempt);
        var first = await repository.ApplyResultAsync(input, default);
        var repeated = await repository.ApplyResultAsync(input, default);
        Assert.Multiple(() =>
        {
            Assert.That(first.Outcome, Is.EqualTo(PaymentApplyOutcome.Applied));
            Assert.That(repeated.Outcome, Is.EqualTo(PaymentApplyOutcome.Duplicate));
            Assert.That(repeated.Payment, Is.EqualTo(first.Payment));
        });
        Assert.That(await AuditsAsync(attempt.Detail.PaymentId), Has.Count.EqualTo(1));
        Assert.That(await InvoiceStatusAsync(attempt), Is.EqualTo("PAID"));
        var retry = await repository.CreateOrGetAsync(attempt.Detail.InvoiceId, attempt.IdempotencyKey, Now.AddYears(1), default);
        Assert.That(retry.Attempt!.Detail.Status, Is.EqualTo("SUCCESS"));
    }

    [Test]
    public async Task CON_PAY_003_ConcurrentDuplicateCallbacksApplyOneTransitionAndAudit()
    {
        var attempt = await CreateAsync();
        var input = Success(attempt);
        var results = await RaceAsync(8, repo => repo.ApplyResultAsync(input, default));
        Assert.Multiple(() =>
        {
            Assert.That(results.Count(result => result.Outcome == PaymentApplyOutcome.Applied), Is.EqualTo(1));
            Assert.That(results.Count(result => result.Outcome == PaymentApplyOutcome.Duplicate), Is.EqualTo(7));
            Assert.That(results.Select(result => result.Payment!.Status), Is.All.EqualTo("SUCCESS"));
        });
        Assert.That(await AuditsAsync(attempt.Detail.PaymentId), Has.Count.EqualTo(1));
        Assert.That(await InvoiceStatusAsync(attempt), Is.EqualTo("PAID"));
    }

    [Test]
    public async Task DBT_PAY_010_ReferenceBoundToOtherPaymentCannotMutateState()
    {
        var first = await CreateAsync();
        var second = await CreateAsync();
        var input = Success(first);
        await repository.ApplyResultAsync(input, default);
        var rejected = await repository.ApplyResultAsync(Success(second) with { TransactionCode = input.TransactionCode }, default);
        Assert.Multiple(() =>
        {
            Assert.That(rejected.Outcome, Is.EqualTo(PaymentApplyOutcome.ReferenceConflict));
            Assert.That(rejected.Payment!.Status, Is.EqualTo("PENDING"));
            Assert.That(rejected.Payment.PaidAt, Is.Null);
            Assert.That(rejected.Payment.TransactionCode, Is.Null);
        });
        Assert.That(await AuditsAsync(second.Detail.PaymentId), Is.Empty);
        Assert.That(await InvoiceStatusAsync(second), Is.EqualTo("UNPAID"));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task DBT_PAY_011_FirstTerminalResultCannotBeReversed(bool successFirst)
    {
        var attempt = await CreateAsync();
        var success = Success(attempt);
        var failure = success with { Status = PaymentFinalStatus.Failed, VerifiedAmount = null, VerifiedPaidAt = null };
        var first = await repository.ApplyResultAsync(successFirst ? success : failure, default);
        var conflict = await repository.ApplyResultAsync(successFirst ? failure : success, default);
        Assert.Multiple(() =>
        {
            Assert.That(conflict.Outcome, Is.EqualTo(PaymentApplyOutcome.TerminalConflict));
            Assert.That(conflict.Payment, Is.EqualTo(first.Payment));
        });
        Assert.That(await InvoiceStatusAsync(attempt), Is.EqualTo(successFirst ? "PAID" : "UNPAID"));
        Assert.That(await AuditsAsync(attempt.Detail.PaymentId), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task DBT_PAY_012_TwoLegitimateSuccessesCountOnePaidInvoice()
    {
        var first = await CreateAsync(type: "RENTAL_FEE");
        var second = await repository.CreateOrGetAsync(first.Detail.InvoiceId, Guid.NewGuid(), Now, default);
        var one = await repository.ApplyResultAsync(Success(first), default);
        var twoInput = Success(second.Attempt!) with { VerifiedPaidAt = ProviderTime.AddMinutes(1) };
        var two = await repository.ApplyResultAsync(twoInput, default);
        Assert.Multiple(() =>
        {
            Assert.That(one.Outcome, Is.EqualTo(PaymentApplyOutcome.Applied));
            Assert.That(two.Outcome, Is.EqualTo(PaymentApplyOutcome.Applied));
            Assert.That(two.Payment!.Status, Is.EqualTo("SUCCESS"));
            Assert.That(two.Payment.TransactionCode, Is.EqualTo(twoInput.TransactionCode));
            Assert.That(two.Payment.PaidAt, Is.EqualTo(twoInput.VerifiedPaidAt!.Value.ToUniversalTime()));
        });
        Assert.That(await AuditsAsync(first.Detail.PaymentId), Has.Count.EqualTo(1));
        Assert.That(await AuditsAsync(second.Attempt!.Detail.PaymentId), Has.Count.EqualTo(1));
        Assert.That(await db.Invoices.Where(row => row.InvoiceId == first.Detail.InvoiceId && row.Status == "PAID")
            .SumAsync(row => row.AmountDue), Is.EqualTo(first.Detail.Amount));
        Assert.That(await db.Payments.Where(row => row.InvoiceId == first.Detail.InvoiceId && row.Status == "SUCCESS")
            .CountAsync(), Is.EqualTo(2));
    }

    [TestCase("DEPOSIT")]
    [TestCase("RENTAL_FEE")]
    public async Task DBT_PAY_013_LateSuccessPreservesCancellationLifecycleAndHasSafeAudit(string type)
    {
        var attempt = await CreateAsync(type);
        var invoice = await db.Invoices.AsNoTracking().SingleAsync(row => row.InvoiceId == attempt.Detail.InvoiceId);
        var reservationId = type == "DEPOSIT" ? invoice.EntityId : await db.Contracts.Where(row => row.ContractId == invoice.EntityId)
            .Select(row => row.ReservationId).SingleAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.Invoice SET Status = 'CANCELLED' WHERE InvoiceId = {invoice.InvoiceId}");
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.Reservation SET Status = 'CANCELLED' WHERE ReservationId = {reservationId}");
        var beforeContract = type == "RENTAL_FEE" ? await db.Contracts.AsNoTracking().SingleAsync(row => row.ContractId == invoice.EntityId) : null;
        var settlementCount = await db.DepositSettlements.CountAsync();
        var url = "https://provider.example.invalid/sensitive/" + Guid.NewGuid().ToString("N");
        await repository.SaveSessionAsync(attempt.Detail.PaymentId, new(null, url, null), Now, default);
        var input = Success(attempt);
        var success = await repository.ApplyResultAsync(input, default);
        var duplicate = await repository.ApplyResultAsync(input, default);
        Assert.Multiple(() =>
        {
            Assert.That(success.Outcome, Is.EqualTo(PaymentApplyOutcome.Applied));
            Assert.That(success.Payment!.Status, Is.EqualTo("SUCCESS"));
            Assert.That(success.Reason, Is.EqualTo("LATE_SUCCESS_ON_CANCELLED_INVOICE"));
            Assert.That(duplicate.Outcome, Is.EqualTo(PaymentApplyOutcome.Duplicate));
        });
        Assert.That(await InvoiceStatusAsync(attempt), Is.EqualTo("CANCELLED"));
        Assert.That(await db.Reservations.Where(row => row.ReservationId == reservationId).Select(row => row.Status).SingleAsync(), Is.EqualTo("CANCELLED"));
        if (beforeContract is not null)
        {
            Assert.That(await db.Contracts.Where(row => row.ContractId == beforeContract.ContractId).Select(row => row.Status).SingleAsync(), Is.EqualTo(beforeContract.Status));
            Assert.That(await db.StorageUnits.Where(row => row.StorageUnitId == beforeContract.StorageUnitId).Select(row => row.Status).SingleAsync(), Is.EqualTo("IN_USE"));
        }
        Assert.That(await db.DepositSettlements.CountAsync(), Is.EqualTo(settlementCount));
        var audits = await AuditsAsync(attempt.Detail.PaymentId);
        Assert.That(audits, Has.Count.EqualTo(1));
        Assert.That(audits[0].NewValue, Does.Contain("LATE_SUCCESS_ON_CANCELLED_INVOICE"));
        var serialized = JsonSerializer.Serialize(audits);
        Assert.That(serialized, Does.Not.Contain(url).And.Not.Contain("signature").And.Not.Contain("secret")
            .And.Not.Contain("RawPayload").And.Not.Contain("token").And.Not.Contain("PaymentUrl"));
        Assert.That(db.Model.GetEntityTypes().Select(row => row.ClrType.Name), Has.None.Contains("Refund"));
    }

    [Test]
    public async Task CON_PAY_004_ConcurrentReferenceClaimHasOneWinnerAndOneConflict()
    {
        var attempts = new[] { await CreateAsync(), await CreateAsync() };
        var reference = Guid.NewGuid().ToString("N");
        var next = -1;
        var results = await RaceAsync(2, repo => repo.ApplyResultAsync(
            Success(attempts[Interlocked.Increment(ref next)]) with { TransactionCode = reference }, default));
        Assert.That(results.Select(result => result.Outcome), Is.EquivalentTo(new[] { PaymentApplyOutcome.Applied, PaymentApplyOutcome.ReferenceConflict }));
        Assert.That(await db.Payments.CountAsync(row => row.TransactionCode == reference), Is.EqualTo(1));
        var ids = attempts.Select(attempt => attempt.Detail.PaymentId).ToArray();
        Assert.That(await db.AuditLogs.CountAsync(row => row.EntityId != null && ids.Contains(row.EntityId.Value)), Is.EqualTo(1));
    }

    [Test]
    public async Task DBT_PAY_014_AuditInsertFailureRollsBackPaymentAndInvoice()
    {
        var attempt = await CreateAsync();
        var id = attempt.Detail.PaymentId;
        // Test-only trigger: force failure after both updates to prove transaction rollback.
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER dbo.PaymentTestAuditFailure ON dbo.AuditLog AFTER INSERT AS
            BEGIN
                IF EXISTS (SELECT 1 FROM inserted WHERE Action = 'PAYMENT_RESULT')
                    THROW 51090, 'Payment test audit failure.', 1;
            END
            """);
        try
        {
            var exception = Assert.ThrowsAsync<SqlException>(async () => await repository.ApplyResultAsync(Success(attempt), default));
            Assert.That(exception!.Number, Is.EqualTo(51090));
            Assert.That((await repository.GetByIdAsync(id, default))!.Status, Is.EqualTo("PENDING"));
            Assert.That(await InvoiceStatusAsync(attempt), Is.EqualTo("UNPAID"));
            Assert.That(await AuditsAsync(id), Is.Empty);
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER dbo.PaymentTestAuditFailure");
        }
    }

    [Test]
    public async Task DBT_PAY_015_UnverifiedOrIncompleteSuccessCannotChangePayment()
    {
        var attempt = await CreateAsync();
        var input = Success(attempt);
        foreach (var invalid in new[]
        {
            input with { VerifiedAmount = null }, input with { VerifiedPaidAt = null },
            input with { TransactionCode = null }, input with { TransactionCode = " " },
            input with { Source = (PaymentResultSource)99 }
        })
        {
            var result = await repository.ApplyResultAsync(invalid, default);
            Assert.That(result.Outcome, Is.EqualTo(PaymentApplyOutcome.InvalidResult));
        }
        Assert.That((await repository.GetByIdAsync(attempt.Detail.PaymentId, default))!.Status, Is.EqualTo("PENDING"));
        Assert.That(await InvoiceStatusAsync(attempt), Is.EqualTo("UNPAID"));
        Assert.That(await AuditsAsync(attempt.Detail.PaymentId), Is.Empty);
    }

    [Test]
    public async Task DAL_PAY_007_SessionCannotChangeTerminalResultOrClaimAnotherReference()
    {
        var attempt = await CreateAsync();
        var success = Success(attempt);
        await repository.ApplyResultAsync(success, default);
        var terminal = await repository.SaveSessionAsync(attempt.Detail.PaymentId, new("changed", "https://provider.example.invalid/ignored", null), Now, default);
        Assert.Multiple(() =>
        {
            Assert.That(terminal.Attempt!.Detail.Status, Is.EqualTo("SUCCESS"));
            Assert.That(terminal.Attempt.Detail.TransactionCode, Is.EqualTo(success.TransactionCode));
            Assert.That(terminal.Attempt.PaymentUrl, Is.Null);
        });
        var second = await CreateAsync();
        var conflict = await repository.SaveSessionAsync(second.Detail.PaymentId, new(success.TransactionCode, "https://provider.example.invalid/rejected", null), Now, default);
        Assert.That(conflict.Outcome, Is.EqualTo(PaymentAttemptOutcome.ReferenceConflict));
        Assert.That(conflict.Attempt, Is.Null);
        Assert.That((await repository.GetByIdempotencyKeyAsync(second.Detail.InvoiceId, second.IdempotencyKey, Now, default)).Attempt!.PaymentUrl, Is.Null);
    }

    private static NormalizedPaymentResult Success(PaymentAttemptRecord attempt) => new(
        attempt.Detail.PaymentId, PaymentFinalStatus.Success, Guid.NewGuid().ToString("N"),
        attempt.Detail.Amount, ProviderTime, PaymentResultSource.VerifiedCallback);
    private Task<string> InvoiceStatusAsync(PaymentAttemptRecord attempt) => db.Invoices
        .Where(row => row.InvoiceId == attempt.Detail.InvoiceId).Select(row => row.Status).SingleAsync();
    private Task<List<Frms.DataAccess.Persistence.Entities.AuditLog>> AuditsAsync(Guid paymentId) => db.AuditLogs.AsNoTracking()
        .Where(row => row.EntityId == paymentId && row.Action == "PAYMENT_RESULT").ToListAsync();
}
