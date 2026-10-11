namespace Frms.DataAccess.Repositories.Models;

// Scope is resolved by Business from the current authoritative account.
public sealed record BillingReadScope(Guid? CustomerId, Guid? FacilityId);

public sealed record InvoiceReadRecord(
    Guid InvoiceId, Guid EntityId, string InvoiceType, DateOnly? BillingMonth,
    decimal BaseAmount, Guid? DiscountId, decimal DiscountAmount, decimal AmountDue,
    DateTime DueDate, string Status, DateTime? PaidAt,
    Guid CustomerId, Guid FacilityId);

public sealed record InvoicePageRecord(IReadOnlyList<InvoiceReadRecord> Items, long TotalItems);
public sealed record BillingContractRecord(Guid ContractId, Guid CustomerId, Guid FacilityId);
public sealed record LateFeeReadRecord(
    Guid LateFeeId, Guid InvoiceId, int OverdueDays, decimal Amount, DateTime CalculatedAt);
public sealed record ContractBillingRecord(
    Guid ContractId, IReadOnlyList<InvoiceReadRecord> Invoices, IReadOnlyList<LateFeeReadRecord> LateFees);
