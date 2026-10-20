namespace Frms.Api.DTOs.Responses;

public sealed record CustomerNotificationResponse(
    Guid NotificationId, string NotificationType, string Content,
    DateTimeOffset CreatedAt, DateTimeOffset SentAt);

public sealed record CustomerPaymentHistoryResponse(
    Guid PaymentId, Guid InvoiceId, string InvoiceType, decimal Amount,
    string PaymentMethod, string Status, DateTimeOffset? PaidAt, DateTimeOffset CreatedAt);
