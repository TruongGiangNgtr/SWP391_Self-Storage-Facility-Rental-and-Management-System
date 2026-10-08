using System.Data;
using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Frms.DataAccess.Repositories.Implementations;

public sealed class PaymentRepository(FrmsDbContext database) : IPaymentRepository
{
    private IQueryable<PaymentInvoiceRecord> InvoiceQuery =>
        from invoice in database.Invoices.AsNoTracking()
        join reservation in database.Reservations on invoice.EntityId equals reservation.ReservationId into reservations
        from reservation in reservations.DefaultIfEmpty()
        join contract in database.Contracts on invoice.EntityId equals contract.ContractId into contracts
        from contract in contracts.DefaultIfEmpty()
        let customerId = invoice.InvoiceType == "DEPOSIT" ? (Guid?)reservation.CustomerId : contract.CustomerId
        join customer in database.Customers on customerId equals (Guid?)customer.CustomerId
        where (invoice.InvoiceType == "DEPOSIT" && reservation != null)
            || (invoice.InvoiceType == "RENTAL_FEE" && contract != null)
        select new PaymentInvoiceRecord
        {
            InvoiceId = invoice.InvoiceId, InvoiceType = invoice.InvoiceType, Status = invoice.Status,
            AmountDue = invoice.AmountDue, CustomerId = customer.CustomerId, CustomerUserAccountId = customer.UserAccountId,
            FacilityId = invoice.InvoiceType == "DEPOSIT" ? reservation.FacilityId : contract.FacilityId,
            BillingMonth = invoice.BillingMonth, ContractStartMonth = invoice.InvoiceType == "RENTAL_FEE" ? contract.StartMonth : null
        };

    public Task<PaymentInvoiceRecord?> GetInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken) =>
        InvoiceQuery.SingleOrDefaultAsync(row => row.InvoiceId == invoiceId, cancellationToken);

    public async Task<PaymentDetailRecord?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        // Select only detail columns: session data must not flow through PAY-003 reads.
        var row = await (from payment in database.Payments.AsNoTracking()
                         join invoice in InvoiceQuery on payment.InvoiceId equals invoice.InvoiceId
                         where payment.PaymentId == paymentId
                         select new
                         {
                             payment.PaymentId, payment.InvoiceId, payment.Amount, payment.PaymentMethod,
                             payment.TransactionCode, payment.Status, payment.PaidAt, payment.CreatedAt,
                             invoice.CustomerId, invoice.CustomerUserAccountId, invoice.FacilityId
                         }).SingleOrDefaultAsync(cancellationToken);
        return row is null ? null : new PaymentDetailRecord(row.PaymentId, row.InvoiceId, row.Amount,
            row.PaymentMethod, row.TransactionCode, row.Status, Utc(row.PaidAt), Utc(row.CreatedAt),
            row.CustomerId, row.CustomerUserAccountId, row.FacilityId);
    }

    public async Task<PaymentAttemptResult> GetByIdempotencyKeyAsync(
        Guid invoiceId, Guid idempotencyKey, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // Check the binding without loading another Invoice's Payment/session into the result.
        var binding = await database.Payments.AsNoTracking()
            .Where(row => row.IdempotencyKey == idempotencyKey)
            .Select(row => new { row.PaymentId, row.InvoiceId }).SingleOrDefaultAsync(cancellationToken);
        if (binding is null) return new(PaymentAttemptOutcome.NotFound, null);
        if (binding.InvoiceId != invoiceId) return new(PaymentAttemptOutcome.IdempotencyConflict, null);
        return await ReadAttemptAsync(binding.PaymentId, now, PaymentAttemptOutcome.Existing, cancellationToken);
    }

    public async Task<PaymentAttemptResult> CreateOrGetAsync(
        Guid invoiceId, Guid idempotencyKey, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var existing = await GetByIdempotencyKeyAsync(invoiceId, idempotencyKey, now, cancellationToken);
        if (existing.Outcome != PaymentAttemptOutcome.NotFound) return existing;

        var paymentId = Guid.NewGuid();
        await using (var transaction = await database.Database.BeginTransactionAsync(cancellationToken))
        {
            try
            {
                // Invoice locks serialize attempt creation with cancellation/payment completion.
                await database.Invoices.FromSqlInterpolated($"SELECT * FROM dbo.Invoice WITH (UPDLOCK, HOLDLOCK) WHERE InvoiceId = {invoiceId}")
                    .AsNoTracking().ToListAsync(cancellationToken);

                // Recheck after acquiring the lock: a concurrent attempt may already have won.
                var binding = await database.Payments.AsNoTracking().Where(row => row.IdempotencyKey == idempotencyKey)
                    .Select(row => new { row.PaymentId, row.InvoiceId }).SingleOrDefaultAsync(cancellationToken);
                if (binding is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                    return binding.InvoiceId != invoiceId
                        ? new(PaymentAttemptOutcome.IdempotencyConflict, null)
                        : await ReadAttemptAsync(binding.PaymentId, now, PaymentAttemptOutcome.Existing, cancellationToken);
                }

                var invoice = await GetInvoiceAsync(invoiceId, cancellationToken);
                if (invoice is null || invoice.Status is not ("UNPAID" or "OVERDUE")
                    || (invoice.InvoiceType != "DEPOSIT"
                        && !(invoice.InvoiceType == "RENTAL_FEE" && invoice.BillingMonth > invoice.ContractStartMonth)))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new(PaymentAttemptOutcome.InvoiceNotPayable, null);
                }

                if (invoice.AmountDue <= 0m)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new(PaymentAttemptOutcome.AmountUnsupported, null);
                }
                await ExecuteAsync("""
                    INSERT INTO dbo.Payment (PaymentId, InvoiceId, IdempotencyKey, Amount, PaymentMethod, Status, CreatedAt)
                    SELECT @Id, InvoiceId, @Key, AmountDue, 'VNPAY', 'PENDING', @Now
                    FROM dbo.Invoice WHERE InvoiceId = @Invoice
                    """, [Parameter("@Id", SqlDbType.UniqueIdentifier, paymentId),
                        Parameter("@Invoice", SqlDbType.UniqueIdentifier, invoiceId),
                        Parameter("@Key", SqlDbType.UniqueIdentifier, idempotencyKey),
                        Parameter("@Now", SqlDbType.DateTime2, now.UtcDateTime)], cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (SqlException exception) when (exception.Number is 2601 or 2627)
            {
                await transaction.RollbackAsync(cancellationToken);
                // Unique IdempotencyKey is the final arbiter across different Invoice locks.
                var winner = await GetByIdempotencyKeyAsync(invoiceId, idempotencyKey, now, cancellationToken);
                if (winner.Outcome == PaymentAttemptOutcome.NotFound) throw;
                return winner;
            }
        }
        return await ReadAttemptAsync(paymentId, now, PaymentAttemptOutcome.Created, cancellationToken);
    }

    public async Task<PaymentAttemptResult> SaveSessionAsync(
        Guid paymentId, PaymentSessionRecord session, DateTimeOffset now, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(session.PaymentUrl);
        if (session.PaymentUrl.Length > 2048) throw new ArgumentOutOfRangeException(nameof(session));
        if (session.TransactionCode is not null
            && (string.IsNullOrWhiteSpace(session.TransactionCode) || session.TransactionCode.Length > 150))
            throw new ArgumentException("Invalid gateway reference length.", nameof(session));

        await using (var transaction = await database.Database.BeginTransactionAsync(cancellationToken))
        {
            // Same short transaction lock as the result procedure. It serializes reference binding,
            // including collations that treat differently spelled provider references as equal.
            await ExecuteAsync("""
                DECLARE @lockResult int;
                EXEC @lockResult = sys.sp_getapplock @Resource = N'FRMS:PaymentReferenceBinding',
                    @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000;
                IF @lockResult < 0 THROW 51000, 'Payment reference lock could not be acquired.', 1;
                """, [], cancellationToken);
            var payment = await database.Payments.FromSqlInterpolated(
                    $"SELECT * FROM dbo.Payment WITH (UPDLOCK, HOLDLOCK) WHERE PaymentId = {paymentId}")
                .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            if (payment is null)
            {
                await transaction.CommitAsync(cancellationToken);
                return new(PaymentAttemptOutcome.NotFound, null);
            }
            if (payment.Status == "PENDING" && payment.PaymentUrl is null)
            {
                if (session.TransactionCode is not null && await database.Payments.AnyAsync(
                        row => row.PaymentId != paymentId && row.TransactionCode == session.TransactionCode, cancellationToken))
                {
                    await transaction.CommitAsync(cancellationToken);
                    return new(PaymentAttemptOutcome.ReferenceConflict, null);
                }
                // Use a DbCommand to keep the provider URL out of EF command logging.
                await ExecuteAsync("""
                    UPDATE dbo.Payment SET TransactionCode = COALESCE(@Reference, TransactionCode),
                        PaymentUrl = @Url, PaymentUrlExpiresAt = @ExpiresAt
                    WHERE PaymentId = @Id
                    """, [Parameter("@Reference", SqlDbType.VarChar, session.TransactionCode, 150),
                        Parameter("@Url", SqlDbType.NVarChar, session.PaymentUrl, 2048),
                        Parameter("@ExpiresAt", SqlDbType.DateTime2, session.ExpiresAt?.UtcDateTime),
                        Parameter("@Id", SqlDbType.UniqueIdentifier, paymentId)], cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
        }
        return await ReadAttemptAsync(paymentId, now, PaymentAttemptOutcome.Existing, cancellationToken);
    }

    public async Task<PaymentApplyResult> ApplyResultAsync(NormalizedPaymentResult result, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(result.Status) || !Enum.IsDefined(result.Source)
            || result.TransactionCode?.Length > 150)
            return new(PaymentApplyOutcome.InvalidResult, null, null);
        var outcome = Parameter("@Outcome", SqlDbType.VarChar, null, 30);
        outcome.Direction = ParameterDirection.Output;
        var reason = Parameter("@Reason", SqlDbType.VarChar, null, 80);
        reason.Direction = ParameterDirection.Output;
        var amount = Parameter("@VerifiedAmount", SqlDbType.Decimal, result.VerifiedAmount);
        // Do not round a mismatching provider amount to the invoice's two-decimal scale.
        amount.Precision = 38;
        amount.Scale = 18;
        await ExecuteAsync("dbo.usp_ApplyPaymentResult",
            [Parameter("@PaymentId", SqlDbType.UniqueIdentifier, result.PaymentId),
             Parameter("@Status", SqlDbType.VarChar, result.Status == PaymentFinalStatus.Success ? "SUCCESS" : "FAILED", 50),
             Parameter("@TransactionCode", SqlDbType.VarChar, result.TransactionCode, 150), amount,
             Parameter("@VerifiedPaidAtUtc", SqlDbType.DateTime2, result.VerifiedPaidAt?.UtcDateTime),
             Parameter("@Source", SqlDbType.VarChar, "VERIFIED_CALLBACK", 50), outcome, reason],
            cancellationToken, CommandType.StoredProcedure);
        var applied = Enum.Parse<PaymentApplyOutcome>((string)outcome.Value, ignoreCase: false);
        return new(applied, await GetByIdAsync(result.PaymentId, cancellationToken), reason.Value as string);
    }

    private async Task<PaymentAttemptResult> ReadAttemptAsync(
        Guid paymentId, DateTimeOffset now, PaymentAttemptOutcome outcome, CancellationToken cancellationToken)
    {
        var payment = await database.Payments.AsNoTracking().SingleAsync(row => row.PaymentId == paymentId, cancellationToken);
        var detail = await GetByIdAsync(paymentId, cancellationToken);
        if (detail is null) return new(PaymentAttemptOutcome.NotFound, null);
        var expiry = Utc(payment.PaymentUrlExpiresAt);
        if (detail.Status == "PENDING" && expiry <= now) outcome = PaymentAttemptOutcome.SessionExpired;
        return new(outcome, new PaymentAttemptRecord(detail, payment.IdempotencyKey, payment.PaymentUrl, expiry));
    }

    private async Task ExecuteAsync(string sql, SqlParameter[] parameters, CancellationToken cancellationToken,
        CommandType type = CommandType.Text)
    {
        var connection = database.Database.GetDbConnection();
        var close = connection.State != ConnectionState.Open;
        try
        {
            if (close) await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = type;
            command.Transaction = database.Database.CurrentTransaction?.GetDbTransaction();
            command.Parameters.AddRange(parameters);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            if (close) await connection.CloseAsync();
        }
    }

    private static SqlParameter Parameter(string name, SqlDbType type, object? value, int size = 0) =>
        new(name, type) { Value = value ?? DBNull.Value, Size = size };
    private static DateTimeOffset Utc(DateTime time) => new(DateTime.SpecifyKind(time, DateTimeKind.Utc));
    private static DateTimeOffset? Utc(DateTime? time) => time.HasValue ? Utc(time.Value) : null;
}
