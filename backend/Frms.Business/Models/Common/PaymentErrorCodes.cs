namespace Frms.Business.Models.Common;

public static class PaymentErrorCodes
{
    public const string InvoiceNotPayable = "INVOICE_NOT_PAYABLE";
    public const string IdempotencyConflict = "PAYMENT_IDEMPOTENCY_CONFLICT";
    public const string SessionExpired = "PAYMENT_SESSION_EXPIRED";
    public const string AmountUnsupported = "PAYMENT_AMOUNT_UNSUPPORTED";
}
