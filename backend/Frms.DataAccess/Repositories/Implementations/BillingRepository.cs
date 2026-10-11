using Frms.DataAccess.Persistence;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class BillingRepository(FrmsDbContext dbContext) : IBillingRepository
{
    public async Task<InvoicePageRecord> ListInvoicesAsync(
        BillingReadScope scope, long skip, int take, CancellationToken cancellationToken)
    {
        var query = ScopedInvoices(scope);
        var totalItems = await query.LongCountAsync(cancellationToken);
        // Skip accepts Int32; an offset beyond it is necessarily past supported row counts.
        IReadOnlyList<InvoiceReadRecord> items = skip > int.MaxValue
            ? []
            : await query.OrderByDescending(x => x.DueDate).ThenBy(x => x.InvoiceId)
                .Skip((int)skip).Take(take).Select(ToRecord).ToListAsync(cancellationToken);
        return new InvoicePageRecord(items, totalItems);
    }

    public Task<InvoiceReadRecord?> GetInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken)
        => ScopedInvoices(new BillingReadScope(null, null))
            .Where(x => x.InvoiceId == invoiceId).Select(ToRecord).SingleOrDefaultAsync(cancellationToken);

    public Task<BillingContractRecord?> GetContractAsync(Guid contractId, CancellationToken cancellationToken)
        => dbContext.Contracts.AsNoTracking().Where(x => x.ContractId == contractId)
            .Select(x => new BillingContractRecord(x.ContractId, x.CustomerId, x.FacilityId))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<ContractBillingRecord?> GetContractBillingAsync(
        Guid contractId, BillingReadScope scope, CancellationToken cancellationToken)
    {
        var contract = dbContext.Contracts.AsNoTracking().Where(x => x.ContractId == contractId
            && (!scope.CustomerId.HasValue || x.CustomerId == scope.CustomerId.Value)
            && (!scope.FacilityId.HasValue || x.FacilityId == scope.FacilityId.Value));
        if (!await contract.AnyAsync(cancellationToken)) return null;

        // Persisted snapshots only, including preserved history. No inferred first-month debt.
        var invoices = ScopedInvoices(scope).Where(x => x.InvoiceType == "RENTAL_FEE"
            && x.EntityId == contractId);
        var items = await invoices.OrderByDescending(x => x.BillingMonth).ThenBy(x => x.InvoiceId)
            .Select(ToRecord).ToListAsync(cancellationToken);
        var lateFees = await (from fee in dbContext.LateFees.AsNoTracking()
                              join invoice in invoices on fee.InvoiceId equals invoice.InvoiceId
                              orderby invoice.BillingMonth descending, fee.LateFeeId
                              select new LateFeeReadRecord(fee.LateFeeId, fee.InvoiceId, fee.OverdueDays,
                                  fee.Amount, fee.CalculatedAt)).ToListAsync(cancellationToken);
        return new ContractBillingRecord(contractId, items, lateFees);
    }

    private IQueryable<InvoiceQueryRow> ScopedInvoices(BillingReadScope scope)
    {
        var deposits = from invoice in dbContext.Invoices.AsNoTracking()
                       join reservation in dbContext.Reservations.AsNoTracking()
                           on invoice.EntityId equals reservation.ReservationId
                       where invoice.InvoiceType == "DEPOSIT"
                           && (!scope.CustomerId.HasValue || reservation.CustomerId == scope.CustomerId.Value)
                           && (!scope.FacilityId.HasValue || reservation.FacilityId == scope.FacilityId.Value)
                       select new InvoiceQueryRow
                       {
                           InvoiceId = invoice.InvoiceId,
                           EntityId = invoice.EntityId,
                           InvoiceType = invoice.InvoiceType,
                           BillingMonth = invoice.BillingMonth,
                           BaseAmount = invoice.BaseAmount,
                           DiscountId = invoice.DiscountId,
                           DiscountAmount = invoice.DiscountAmount,
                           AmountDue = invoice.AmountDue,
                           DueDate = invoice.DueDate,
                           Status = invoice.Status,
                           PaidAt = invoice.PaidAt,
                           CustomerId = reservation.CustomerId,
                           FacilityId = reservation.FacilityId
                       };
        var rentals = from invoice in dbContext.Invoices.AsNoTracking()
                      join contract in dbContext.Contracts.AsNoTracking() on invoice.EntityId equals contract.ContractId
                      where invoice.InvoiceType == "RENTAL_FEE"
                          && (!scope.CustomerId.HasValue || contract.CustomerId == scope.CustomerId.Value)
                          && (!scope.FacilityId.HasValue || contract.FacilityId == scope.FacilityId.Value)
                      select new InvoiceQueryRow
                      {
                          InvoiceId = invoice.InvoiceId,
                          EntityId = invoice.EntityId,
                          InvoiceType = invoice.InvoiceType,
                          BillingMonth = invoice.BillingMonth,
                          BaseAmount = invoice.BaseAmount,
                          DiscountId = invoice.DiscountId,
                          DiscountAmount = invoice.DiscountAmount,
                          AmountDue = invoice.AmountDue,
                          DueDate = invoice.DueDate,
                          Status = invoice.Status,
                          PaidAt = invoice.PaidAt,
                          CustomerId = contract.CustomerId,
                          FacilityId = contract.FacilityId
                      };
        return deposits.Concat(rentals);
    }

    // Keep the UNION ALL and all predicates server-translatable; construct the public
    // persistence record only after filtering, sorting and paging have been applied.
    private static readonly Expression<Func<InvoiceQueryRow, InvoiceReadRecord>> ToRecord = x => new(
        x.InvoiceId, x.EntityId, x.InvoiceType, x.BillingMonth, x.BaseAmount, x.DiscountId,
        x.DiscountAmount, x.AmountDue, x.DueDate, x.Status, x.PaidAt, x.CustomerId, x.FacilityId);

    private sealed class InvoiceQueryRow
    {
        public Guid InvoiceId { get; init; }
        public Guid EntityId { get; init; }
        public required string InvoiceType { get; init; }
        public DateOnly? BillingMonth { get; init; }
        public decimal BaseAmount { get; init; }
        public Guid? DiscountId { get; init; }
        public decimal DiscountAmount { get; init; }
        public decimal AmountDue { get; init; }
        public DateTime DueDate { get; init; }
        public required string Status { get; init; }
        public DateTime? PaidAt { get; init; }
        public Guid CustomerId { get; init; }
        public Guid FacilityId { get; init; }
    }
}
