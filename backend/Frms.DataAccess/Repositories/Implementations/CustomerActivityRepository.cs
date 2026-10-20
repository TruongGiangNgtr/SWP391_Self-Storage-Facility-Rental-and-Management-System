using Frms.DataAccess.Persistence;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class CustomerActivityRepository(FrmsDbContext db) : ICustomerActivityRepository
{
    public Task<Guid?> ResolveActiveCustomerIdAsync(Guid userAccountId, CancellationToken ct) =>
        (from c in db.Customers.AsNoTracking()
         join a in db.UserAccounts.AsNoTracking() on c.UserAccountId equals a.UserAccountId
         join role in db.UserRoles.AsNoTracking() on a.RoleId equals role.RoleId
         where a.UserAccountId == userAccountId && a.Status == "ACTIVE" && role.RoleName == "CUSTOMER"
         select (Guid?)c.CustomerId).SingleOrDefaultAsync(ct);

    public async Task<CustomerActivityPage<CustomerNotificationRecord>> ListSentNotificationsAsync(
        Guid userAccountId, long skip, int take, CancellationToken ct)
    {
        // NotificationLog is a delivery queue, not a read/unread inbox.
        // Never surface PENDING/unsent messages or credentials from unvalidated notification types.
        var query = db.NotificationLogs.AsNoTracking()
            .Where(n => n.UserAccountId == userAccountId && n.Status == "SENT" && n.SentAt != null);
        var count = await query.LongCountAsync(ct);
        if (skip > int.MaxValue) return new CustomerActivityPage<CustomerNotificationRecord>([], count);
        var rows = await query.OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.NotificationLogId)
            .Skip((int)skip).Take(take)
            .Select(n => new CustomerNotificationRecord(
                n.NotificationLogId, n.NotificationType, n.Content, n.CreatedAt, n.SentAt!.Value))
            .ToListAsync(ct);
        return new CustomerActivityPage<CustomerNotificationRecord>(rows, count);
    }

    public async Task<CustomerActivityPage<CustomerPaymentHistoryRecord>> ListPaymentsAsync(
        Guid customerId, long skip, int take, CancellationToken ct)
    {
        // Identity is derived from authenticated UserAccount -> Customer.
        // Every payment is associated through its Invoice to an owned Reservation or Contract.
        var deposits =
            from payment in db.Payments.AsNoTracking()
            join invoice in db.Invoices.AsNoTracking() on payment.InvoiceId equals invoice.InvoiceId
            join reservation in db.Reservations.AsNoTracking() on invoice.EntityId equals reservation.ReservationId
            where invoice.InvoiceType == "DEPOSIT" && reservation.CustomerId == customerId
            select new PaymentRow
            {
                PaymentId = payment.PaymentId, InvoiceId = payment.InvoiceId,
                InvoiceType = invoice.InvoiceType, Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod, Status = payment.Status,
                PaidAt = payment.PaidAt, CreatedAt = payment.CreatedAt
            };
        var rentals =
            from payment in db.Payments.AsNoTracking()
            join invoice in db.Invoices.AsNoTracking() on payment.InvoiceId equals invoice.InvoiceId
            join contract in db.Contracts.AsNoTracking() on invoice.EntityId equals contract.ContractId
            where invoice.InvoiceType == "RENTAL_FEE" && contract.CustomerId == customerId
            select new PaymentRow
            {
                PaymentId = payment.PaymentId, InvoiceId = payment.InvoiceId,
                InvoiceType = invoice.InvoiceType, Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod, Status = payment.Status,
                PaidAt = payment.PaidAt, CreatedAt = payment.CreatedAt
            };
        var query = deposits.Concat(rentals);
        var count = await query.LongCountAsync(ct);
        if (skip > int.MaxValue) return new CustomerActivityPage<CustomerPaymentHistoryRecord>([], count);
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.PaymentId)
            .Skip((int)skip).Take(take)
            .Select(x => new CustomerPaymentHistoryRecord(
                x.PaymentId, x.InvoiceId, x.InvoiceType, x.Amount, x.PaymentMethod,
                x.Status, x.PaidAt, x.CreatedAt))
            .ToListAsync(ct);
        // Offline first-month rent is a Contract business record, not a Payment. Do not fabricate it here.
        return new CustomerActivityPage<CustomerPaymentHistoryRecord>(items, count);
    }

    private sealed class PaymentRow
    {
        public Guid PaymentId { get; init; }
        public Guid InvoiceId { get; init; }
        public required string InvoiceType { get; init; }
        public decimal Amount { get; init; }
        public required string PaymentMethod { get; init; }
        public required string Status { get; init; }
        public DateTime? PaidAt { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
