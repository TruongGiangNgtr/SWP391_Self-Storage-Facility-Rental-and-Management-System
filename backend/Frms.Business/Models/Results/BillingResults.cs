namespace Frms.Business.Models.Results;

public sealed record InvoiceResult(
    Guid InvoiceId, Guid EntityId, string InvoiceType, DateOnly? BillingMonth,
    decimal BaseAmount, Guid? DiscountId, decimal DiscountAmount, decimal AmountDue,
    DateTimeOffset DueDate, string Status, DateTimeOffset? PaidAt);
public sealed record InvoicePageResult(
    IReadOnlyList<InvoiceResult> Items, int Page, int PageSize, long TotalItems, int TotalPages);
public sealed record LateFeeResult(
    Guid LateFeeId, Guid InvoiceId, int OverdueDays, decimal Amount, DateTimeOffset CalculatedAt);
public sealed record ContractBillingResult(
    Guid ContractId, IReadOnlyList<InvoiceResult> Invoices, IReadOnlyList<LateFeeResult> LateFees);
