using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Frms.Business.Abstractions.External;
using Frms.Business.Abstractions.Time;
using Frms.Business.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Frms.Infrastructure.Payments;

/// <summary>payOS wire contract stays in Infrastructure. One HTTP create, no automatic retry/registration.</summary>
public sealed class PayOsPaymentGateway(HttpClient http, IOptions<PayOsOptions> options, IClock clock,
    ILogger<PayOsPaymentGateway>? logger = null) : IPaymentGateway
{
    private const int MaximumPayloadBytes = 65_536;
    private static readonly TimeSpan ProviderOffset = TimeSpan.FromHours(7);

    public Task EnsureConfiguredAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = Configuration();
        return Task.CompletedTask;
    }

    public void ValidatePaymentRequest(PaymentGatewayPreflight request)
    {
        var config = Configuration();
        if (!Amount(request.Amount))
            throw new BusinessException("PAYMENT_AMOUNT_UNSUPPORTED", "The Invoice amount must be positive integral VND.", 409);
        _ = Expiry(clock.UtcNow, config.ExpiryMinutes);
        // No caller-controlled URL/IP is used in the payOS request.
    }

    public async Task<PaymentGatewayCreationResult> CreatePaymentAsync(PaymentGatewayRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidatePaymentRequest(new(request.Amount, request.ReturnUrl, request.ClientIpAddress));
        if (request.PaymentId == Guid.Empty || request.ProviderOrderCode is not >= 1000)
            throw new BusinessException("VALIDATION_ERROR", "The payment request is invalid.", 400);
        var config = Configuration();
        var expires = Expiry(request.CreatedAt, config.ExpiryMinutes);
        if (expires <= clock.UtcNow) return Unavailable("request_expired", request);
        var orderCode = request.ProviderOrderCode.Value;
        var amount = decimal.ToInt64(request.Amount);
        var canonical = $"amount={amount.ToString(CultureInfo.InvariantCulture)}&cancelUrl={config.CancelUrl}&description=FRMS&orderCode={orderCode.ToString(CultureInfo.InvariantCulture)}&returnUrl={config.ReturnUrl}";
        using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(config.ApiBaseUrl), "/v2/payment-requests"));
        message.Headers.Add("x-client-id", config.ClientId);
        message.Headers.Add("x-api-key", config.ApiKey);
        message.Content = JsonContent.Create(new
        {
            orderCode, amount, description = "FRMS", returnUrl = config.ReturnUrl, cancelUrl = config.CancelUrl,
            expiredAt = expires.ToUnixTimeSeconds(), signature = PayOsSignature.Sign(canonical, config.ChecksumKey)
        });
        int? httpStatus = null;
        string? providerCode = null;
        var stage = "http_send";
        try
        {
            using var response = await http.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            httpStatus = (int)response.StatusCode;
            stage = "response_read";
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var buffer = new MemoryStream();
            var block = new byte[4096];
            int count;
            while ((count = await stream.ReadAsync(block, cancellationToken)) != 0)
            {
                if (buffer.Length + count > MaximumPayloadBytes)
                    return Unavailable("response_too_large", request, httpStatus);
                buffer.Write(block, 0, count);
            }
            stage = "response_json";
            using var json = JsonDocument.Parse(buffer.ToArray(), new() { MaxDepth = 32 });
            var root = json.RootElement;
            stage = "response_duplicate_fields";
            PayOsSignature.RejectDuplicates(root);
            providerCode = Text(root, "code");
            if (!response.IsSuccessStatusCode) return Unavailable("http_status", request, httpStatus, providerCode);
            if (providerCode != "00") return Unavailable("provider_code", request, httpStatus, providerCode);
            if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
                return Unavailable("response_data", request, httpStatus, providerCode);
            stage = "response_signature";
            if (!PayOsSignature.Verify(data, Text(root, "signature"), config.ChecksumKey))
                return Unavailable(stage, request, httpStatus, providerCode);
            stage = "response_fields";
            if (Number(data, "orderCode") != orderCode) return Unavailable("response_order_code", request, httpStatus, providerCode);
            if (Number(data, "amount") != amount) return Unavailable("response_amount", request, httpStatus, providerCode);
            if (Text(data, "currency") != "VND") return Unavailable("response_currency", request, httpStatus, providerCode);
            if (Text(data, "status") != "PENDING") return Unavailable("response_status", request, httpStatus, providerCode);
            stage = "response_checkout_url";
            var url = Text(data, "checkoutUrl");
            if (url is null || url.Length > 2048 || !Uri.TryCreate(url, UriKind.Absolute, out var uri)
                || uri.Scheme != Uri.UriSchemeHttps || uri.UserInfo.Length != 0)
                return Unavailable("response_checkout_url", request, httpStatus, providerCode);
            stage = "response_expiry";
            if (data.TryGetProperty("expiredAt", out var expiry) && expiry.ValueKind != JsonValueKind.Null)
            {
                if (!expiry.TryGetInt32(out var seconds) || seconds <= 0)
                    return Unavailable("response_expiry", request, httpStatus, providerCode);
                expires = DateTimeOffset.FromUnixTimeSeconds(seconds);
            }
            if (expires <= clock.UtcNow) return Unavailable("response_expired", request, httpStatus, providerCode);
            return new(PaymentGatewayCreationOutcome.SessionCreated, new(url, null, expires), null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or FormatException or InvalidOperationException or OverflowException or ArgumentOutOfRangeException)
        {
            return Unavailable(ex is HttpRequestException ? "transport_error"
                : ex is OperationCanceledException ? "timeout" : stage,
                request, httpStatus, providerCode, ex.GetType().Name);
        }
    }

    private PaymentGatewayCreationResult Unavailable(string stage, PaymentGatewayRequest request,
        int? httpStatus = null, string? providerCode = null, string? exceptionType = null)
    {
        // Only short numeric provider codes are allowed into logs. Never pass raw
        // response text, descriptions, headers, signatures, URLs or exceptions.
        var safeCode = providerCode is null ? "MISSING"
            : providerCode.Length is >= 2 and <= 5 && providerCode.All(char.IsAsciiDigit)
                ? providerCode : "UNRECOGNIZED";
        logger?.LogWarning(new EventId(47001, "PayOsSessionUnavailable"),
            "payOS session unavailable at {Stage}; HTTP status {ProviderHttpStatus}; provider code {ProviderCode}; Payment {PaymentId}; order {ProviderOrderCode}; exception type {ExceptionType}.",
            stage, httpStatus, safeCode, request.PaymentId, request.ProviderOrderCode, exceptionType);
        return Unknown();
    }

    public Task<PaymentGatewayCallbackResult> VerifyAndNormalizeCallbackAsync(PaymentGatewayCallbackRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var config = Configuration();
        if (string.IsNullOrWhiteSpace(request.RawPayload) || Encoding.UTF8.GetByteCount(request.RawPayload) > MaximumPayloadBytes)
            return Task.FromResult(Rejected());
        try
        {
            using var json = JsonDocument.Parse(request.RawPayload, new() { MaxDepth = 32 });
            var root = json.RootElement;
            PayOsSignature.RejectDuplicates(root);
            if (!root.TryGetProperty("data", out var data)
                || !PayOsSignature.Verify(data, Text(root, "signature"), config.ChecksumKey)) return Task.FromResult(Rejected());
            if (Text(root, "code") != "00" || !root.TryGetProperty("success", out var success) || success.ValueKind != JsonValueKind.True
                || Text(data, "code") != "00" || Text(data, "currency") != "VND"
                || Number(data, "orderCode") is not { } orderCode || orderCode <= 0
                || Number(data, "amount") is not { } amount || amount <= 0
                || Text(data, "reference") is not { } reference || reference.Length is 0 or > 150
                || reference.Any(c => c < 33 || c > 126)
                || !DateTime.TryParseExact(Text(data, "transactionDateTime"), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var providerTime)) return Task.FromResult(Rejected());
            var paidAt = new DateTimeOffset(DateTime.SpecifyKind(providerTime, DateTimeKind.Unspecified), ProviderOffset).ToUniversalTime();
            return Task.FromResult(new PaymentGatewayCallbackResult(Guid.Empty, amount, reference, PaymentGatewayCallbackOutcome.Success, paidAt, orderCode));
        }
        catch (Exception ex) when (ex is JsonException or FormatException or InvalidOperationException or OverflowException or ArgumentOutOfRangeException)
        {
            return Task.FromResult(Rejected());
        }
    }

    private PayOsOptions Configuration()
    {
        try
        {
            var config = options.Value;
            if (new PayOsOptionsValidator().Validate(null, config).Failed) throw NotConfigured();
            return config;
        }
        catch (Exception ex) when (ex is OptionsValidationException or InvalidOperationException or FormatException or OverflowException)
        {
            // Binding errors can contain the original configuration value; never expose them.
            throw NotConfigured();
        }
    }

    private static BusinessException NotConfigured() => new("EXTERNAL_PROVIDER_NOT_CONFIGURED", "The payment provider is not configured.", 503);
    private static bool Amount(decimal value) => value > 0 && value <= long.MaxValue && decimal.Truncate(value) == value;
    private static DateTimeOffset Expiry(DateTimeOffset createdAt, int minutes)
    {
        try
        {
            var expiry = createdAt.ToUniversalTime().AddMinutes(minutes);
            if (expiry.ToUnixTimeSeconds() is <= 0 or > int.MaxValue) throw NotConfigured();
            return DateTimeOffset.FromUnixTimeSeconds(expiry.ToUnixTimeSeconds());
        }
        catch (ArgumentOutOfRangeException) { throw NotConfigured(); }
    }
    private static string? Text(JsonElement data, string key) => data.ValueKind == JsonValueKind.Object
        && data.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static long? Number(JsonElement data, string key) => data.ValueKind == JsonValueKind.Object
        && data.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number) ? number : null;
    private static PaymentGatewayCreationResult Unknown() => new(PaymentGatewayCreationOutcome.Unknown, null, null);
    private static PaymentGatewayCallbackResult Rejected() => new(Guid.Empty, null, null, PaymentGatewayCallbackOutcome.Unverified, null);
}
