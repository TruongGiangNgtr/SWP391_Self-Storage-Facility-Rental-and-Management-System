namespace Frms.DataAccess.Repositories.Models;

public sealed record CustomerNotificationRecord(
    Guid NotificationLogId, string NotificationType, string Content,
    DateTime CreatedAt);

public sealed record CustomerPaymentHistoryRecord(
    Guid PaymentId, Guid InvoiceId, string InvoiceType, decimal Amount,
    string PaymentMethod, string Status, DateTime? PaidAt, DateTime CreatedAt);

public sealed record CustomerActivityPage<T>(IReadOnlyList<T> Items, long TotalItems);
