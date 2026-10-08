using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Frms.Business.Abstractions.External;
using Frms.Business.Exceptions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Frms.Infrastructure.Payments;

public sealed class VnPayPaymentGateway(IOptions<VnPayOptions> options) : IPaymentGateway
{
    private const string Version = "2.1.0";
    private const string Command = "pay";
    private const string Currency = "VND";
    private const long MaximumWireAmount = 999_999_999_999;
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    public Task EnsureConfiguredAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = Configuration();
        return Task.CompletedTask;
    }

    public void ValidatePaymentRequest(PaymentGatewayPreflight request)
    {
        var config = Configuration();
        if (!TryWireAmount(request.Amount, out _))
            throw new BusinessException("PAYMENT_AMOUNT_UNSUPPORTED", "The Invoice amount is not supported for payment initiation.", 409);
        if (!string.Equals(request.ReturnUrl, config.ReturnUrl, StringComparison.Ordinal)
            || NormalizeIp(request.ClientIpAddress) is null)
            throw new BusinessException("VALIDATION_ERROR", "The payment request is invalid.", 400);
    }

    public Task<PaymentGatewayCreationResult> CreatePaymentAsync(
        PaymentGatewayRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidatePaymentRequest(new(request.Amount, request.ReturnUrl, request.ClientIpAddress));
        if (request.PaymentId == Guid.Empty)
            throw new BusinessException("VALIDATION_ERROR", "The payment request is invalid.", 400);

        var config = Configuration();
        var transactionReference = request.PaymentId.ToString("N");
        var createdAt = request.CreatedAt.ToUniversalTime();
        var expiresAt = createdAt.AddMinutes(config.ExpiryMinutes);
        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Amount"] = WireAmount(request.Amount),
            ["vnp_Command"] = Command,
            ["vnp_CreateDate"] = ProviderTimestamp(createdAt),
            ["vnp_CurrCode"] = Currency,
            ["vnp_ExpireDate"] = ProviderTimestamp(expiresAt),
            ["vnp_IpAddr"] = NormalizeIp(request.ClientIpAddress)!,
            ["vnp_Locale"] = config.Locale,
            ["vnp_OrderInfo"] = $"FRMS payment {transactionReference}",
            ["vnp_OrderType"] = config.OrderType,
            ["vnp_ReturnUrl"] = config.ReturnUrl,
            ["vnp_TmnCode"] = config.TmnCode,
            ["vnp_TxnRef"] = transactionReference,
            ["vnp_Version"] = Version
        };
        var signedData = Encode(fields);
        var paymentUrl = $"{config.PaymentUrl}?{signedData}&vnp_SecureHash={Sign(signedData, config.HashSecret)}";
        if (paymentUrl.Length > 2048)
            throw new BusinessException("VALIDATION_ERROR", "The payment request is invalid.", 400);

        return Task.FromResult(new PaymentGatewayCreationResult(
            PaymentGatewayCreationOutcome.SessionCreated,
            new PaymentGatewaySession(paymentUrl, null, expiresAt),
            null));
    }

    public Task<PaymentGatewayCallbackResult> VerifyAndNormalizeCallbackAsync(
        PaymentGatewayCallbackRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var config = Configuration();
        return Task.FromResult(Verify(request.RawPayload, config));
    }

    private static PaymentGatewayCallbackResult Verify(string rawPayload, VnPayOptions config)
    {
        if (string.IsNullOrWhiteSpace(rawPayload) || Encoding.UTF8.GetByteCount(rawPayload) > 65_536)
            return Rejected();

        var parsed = QueryHelpers.ParseQuery(rawPayload);
        if (parsed.Count == 0 || parsed.Any(field => field.Value.Count != 1)) return Rejected();
        var values = parsed
            .Where(field => field.Key.StartsWith("vnp_", StringComparison.Ordinal)
                && !string.IsNullOrEmpty(field.Value[0]))
            .ToDictionary(field => field.Key, field => field.Value[0]!, StringComparer.Ordinal);
        if (!values.Remove("vnp_SecureHash", out var suppliedHash)
            || suppliedHash.Length != 128)
            return Rejected();
        values.Remove("vnp_SecureHashType");

        try
        {
            var signedData = Encode(new SortedDictionary<string, string>(values, StringComparer.Ordinal));
            var expected = Convert.FromHexString(Sign(signedData, config.HashSecret));
            var supplied = Convert.FromHexString(suppliedHash);
            if (!CryptographicOperations.FixedTimeEquals(expected, supplied)) return Rejected();
        }
        catch (FormatException)
        {
            return Rejected();
        }

        if (!Value(values, "vnp_TmnCode", out var merchant)
            || !string.Equals(merchant, config.TmnCode, StringComparison.Ordinal)
            || !Value(values, "vnp_TxnRef", out var reference)
            || !Guid.TryParseExact(reference, "N", out var paymentId)
            || paymentId == Guid.Empty
            || !Value(values, "vnp_Amount", out var amountText)
            || !long.TryParse(amountText, NumberStyles.None, CultureInfo.InvariantCulture, out var wireAmount)
            || wireAmount <= 0
            || !Value(values, "vnp_ResponseCode", out var responseCode)
            || !Value(values, "vnp_TransactionStatus", out var transactionStatus))
            return Unknown();

        var amount = wireAmount / 100m;
        var success = responseCode == "00" && transactionStatus == "00";
        if (!Value(values, "vnp_TransactionNo", out var transactionCode)
            || transactionCode == "0" || transactionCode.Length > 150
            || !transactionCode.All(char.IsAsciiDigit))
            return Unknown();

        DateTimeOffset? paidAt = null;
        if (success)
        {
            if (!Value(values, "vnp_PayDate", out var payDate)
                || !DateTime.TryParseExact(payDate, "yyyyMMddHHmmss", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var localPaidAt))
                return Unknown();
            paidAt = new DateTimeOffset(DateTime.SpecifyKind(localPaidAt, DateTimeKind.Unspecified), VietnamOffset)
                .ToUniversalTime();
        }

        return new(paymentId, amount, transactionCode,
            success ? PaymentGatewayCallbackOutcome.Success : PaymentGatewayCallbackOutcome.Failed,
            paidAt);
    }

    private VnPayOptions Configuration()
    {
        try
        {
            return options.Value;
        }
        catch (OptionsValidationException)
        {
            throw new BusinessException("EXTERNAL_PROVIDER_NOT_CONFIGURED", "Payment gateway is not configured.", 503);
        }
    }

    private static string Encode(IEnumerable<KeyValuePair<string, string>> fields) => string.Join("&",
        fields.Select(field => $"{WebUtility.UrlEncode(field.Key)}={WebUtility.UrlEncode(field.Value)}"));

    private static string Sign(string data, string secret) => Convert.ToHexStringLower(
        HMACSHA512.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(data)));

    private static string ProviderTimestamp(DateTimeOffset value) =>
        value.ToOffset(VietnamOffset).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

    private static string WireAmount(decimal amount) =>
        decimal.ToInt64(amount * 100m).ToString(CultureInfo.InvariantCulture);

    private static bool TryWireAmount(decimal amount, out long wireAmount)
    {
        wireAmount = 0;
        var scaled = amount * 100m;
        if (amount <= 0m || scaled != decimal.Truncate(scaled) || scaled > MaximumWireAmount)
            return false;
        wireAmount = decimal.ToInt64(scaled);
        return true;
    }

    private static string? NormalizeIp(string value)
    {
        if (!IPAddress.TryParse(value, out var address)) return null;
        if (IPAddress.IsLoopback(address)) return IPAddress.Loopback.ToString();
        if (address.AddressFamily == AddressFamily.InterNetworkV6 && address.IsIPv4MappedToIPv6)
            return address.MapToIPv4().ToString();
        return address.ToString();
    }

    private static bool Value(IReadOnlyDictionary<string, string> values, string key, out string value) =>
        values.TryGetValue(key, out value!) && !string.IsNullOrWhiteSpace(value);

    private static PaymentGatewayCallbackResult Rejected() =>
        new(Guid.Empty, null, null, PaymentGatewayCallbackOutcome.Unverified, null);

    private static PaymentGatewayCallbackResult Unknown() =>
        new(Guid.Empty, null, null, PaymentGatewayCallbackOutcome.Unknown, null);
}
