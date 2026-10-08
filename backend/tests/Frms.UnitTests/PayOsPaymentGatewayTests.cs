using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Frms.Business.Abstractions.External;
using Frms.Business.Abstractions.Time;
using Frms.Business.Exceptions;
using Frms.Infrastructure.Payments;
using Microsoft.Extensions.Options;

namespace Frms.UnitTests;

[TestFixture, Category("PayOsGateway")]
public sealed class PayOsPaymentGatewayTests
{
    private const string FixtureKey = "fixture-checksum-key-not-a-secret";
    private const string CreateVector = "1e256391f2b109cd58189a76cd975c613e5d7f4a4b8b6df79200423f576e1017";
    private const string CallbackVector = "a55325079aa471cfb0c4eb73613d78eda1895ab9e926a0c88e52c17a398df32b";
    private static readonly DateTimeOffset Now = new(2026, 10, 7, 10, 0, 0, TimeSpan.Zero);
    private Handler handler = null!;
    private HttpClient http = null!;
    private PayOsOptions config = null!;
    private PayOsPaymentGateway gateway = null!;

    [SetUp]
    public void SetUp()
    {
        handler = new(); http = new(handler);
        config = new() { ClientId = "fixture-client", ApiKey = "fixture-api-key", ChecksumKey = FixtureKey };
        gateway = new(http, Options.Create(config), new Clock());
    }
    [TearDown] public void TearDown() => http.Dispose();

    [Test]
    public async Task UT_PAYOS_CreateFixedSignatureServerUrlsAndExpiryProducePendingSessionOnly()
    {
        var result = await gateway.CreatePaymentAsync(Request(), default);
        using var sent = JsonDocument.Parse(handler.Body!);
        var root = sent.RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(handler.Calls, Is.EqualTo(1));
            Assert.That(handler.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(handler.Url, Is.EqualTo("https://api-merchant.payos.vn/v2/payment-requests"));
            Assert.That(root.GetProperty("signature").GetString(), Is.EqualTo(CreateVector));
            Assert.That(root.GetProperty("orderCode").GetInt64(), Is.EqualTo(1000));
            Assert.That(root.GetProperty("amount").GetInt64(), Is.EqualTo(12500));
            Assert.That(root.GetProperty("description").GetString(), Is.EqualTo("FRMS"));
            Assert.That(root.GetProperty("returnUrl").GetString(), Is.EqualTo(config.ReturnUrl));
            Assert.That(root.GetProperty("cancelUrl").GetString(), Is.EqualTo(config.CancelUrl));
            Assert.That(root.GetProperty("expiredAt").GetInt64(), Is.EqualTo(Now.AddMinutes(15).ToUnixTimeSeconds()));
            Assert.That(handler.ClientId, Is.EqualTo(config.ClientId));
            Assert.That(handler.ApiKey, Is.EqualTo(config.ApiKey));
            Assert.That(result.Outcome, Is.EqualTo(PaymentGatewayCreationOutcome.SessionCreated));
            Assert.That(result.Session!.ExpiresAt, Is.EqualTo(Now.AddMinutes(15)));
            Assert.That(result.Session.TransactionCode, Is.Null);
            Assert.That(result.TransactionCode, Is.Null);
        });
    }

    [TestCase(0)] [TestCase(-1)] [TestCase(0.01)] [TestCase(12500.50)]
    public void UT_PAYOS_InvalidVndRejectedBeforeAnyHttpCall(decimal amount)
    {
        var error = Assert.Throws<BusinessException>(() => gateway.ValidatePaymentRequest(new(amount, "https://attacker.invalid", "ignored")));
        Assert.That(error!.Code, Is.EqualTo("PAYMENT_AMOUNT_UNSUPPORTED"));
        Assert.That(handler.Calls, Is.Zero);
    }

    [TestCase(null)] [TestCase(123)] [TestCase(0)]
    public void UT_PAYOS_OrderCodeMustHaveBeenAllocatedByRepository(long? code)
    {
        Assert.ThrowsAsync<BusinessException>(() => gateway.CreatePaymentAsync(Request() with { ProviderOrderCode = code }, default));
        Assert.That(handler.Calls, Is.Zero);
    }

    [TestCase("orderCode", "9999")]
    [TestCase("amount", "12501")]
    [TestCase("currency", "USD")]
    [TestCase("status", "PAID")]
    [TestCase("status", "FAILED")]
    [TestCase("checkoutUrl", "http://pay.payos.vn/web/fixture")]
    [TestCase("checkoutUrl", "https://user:password@pay.payos.vn/web/fixture")]
    [TestCase("expiredAt", "1")]
    [TestCase("expiredAt", "9223372036854775807")]
    public async Task UT_PAYOS_InvalidSignedCreateResponseCannotProduceSession(string field, string value)
    {
        handler.Data[field] = field is "orderCode" or "amount" or "expiredAt" ? long.Parse(value, CultureInfo.InvariantCulture) : value;
        var result = await gateway.CreatePaymentAsync(Request(), default);
        Assert.That(result.Outcome, Is.EqualTo(PaymentGatewayCreationOutcome.Unknown));
        Assert.That(result.Session, Is.Null);
        Assert.That(handler.Calls, Is.EqualTo(1));
    }

    [TestCase("invalid-signature")]
    [TestCase("invalid-json")]
    [TestCase("provider-error")]
    [TestCase("http-error")]
    [TestCase("transport-error")]
    [TestCase("timeout")]
    public async Task UT_PAYOS_UnknownOutcomeDoesNotRetryOrFabricateSession(string scenario)
    {
        handler.Scenario = scenario;
        var result = await gateway.CreatePaymentAsync(Request(), default);
        Assert.That(result.Outcome, Is.EqualTo(PaymentGatewayCreationOutcome.Unknown));
        Assert.That(result.Session, Is.Null);
        Assert.That(handler.Calls, Is.EqualTo(1));
    }

    [Test]
    public async Task UT_PAYOS_ProviderExpiryWhenAvailableIsPersistedInUtc()
    {
        handler.Data["expiredAt"] = Now.AddMinutes(8).ToUnixTimeSeconds();
        var result = await gateway.CreatePaymentAsync(Request(), default);
        Assert.That(result.Session!.ExpiresAt, Is.EqualTo(Now.AddMinutes(8)));
        Assert.That(result.Session.ExpiresAt!.Value.Offset, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void UT_PAYOS_Int32ExpiryOverflowIsRejectedLocally()
    {
        config.ExpiryMinutes = int.MaxValue;
        var error = Assert.Throws<BusinessException>(() => gateway.ValidatePaymentRequest(new(12500, "", "")));
        Assert.That(error!.Code, Is.EqualTo("EXTERNAL_PROVIDER_NOT_CONFIGURED"));
        Assert.That(handler.Calls, Is.Zero);
    }

    [Test]
    public void UT_PAYOS_CancelledCreateDoesNotMakeHttpRequest()
    {
        using var source = new CancellationTokenSource(); source.Cancel();
        Assert.CatchAsync<OperationCanceledException>(() => gateway.CreatePaymentAsync(Request(), source.Token));
        Assert.That(handler.Calls, Is.Zero);
    }

    [Test]
    public async Task UT_PAYOS_IndependentWebhookVectorMapsReferenceAndUtcPlusSeven()
    {
        var result = await gateway.VerifyAndNormalizeCallbackAsync(new(Webhook()), default);
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(PaymentGatewayCallbackOutcome.Success));
            Assert.That(result.ProviderOrderCode, Is.EqualTo(1000));
            Assert.That(result.PaymentId, Is.EqualTo(Guid.Empty));
            Assert.That(result.Amount, Is.EqualTo(12500m));
            Assert.That(result.TransactionCode, Is.EqualTo("TF900001"));
            Assert.That(result.PaidAt, Is.EqualTo(new DateTimeOffset(2026, 10, 7, 10, 2, 3, TimeSpan.Zero)));
            Assert.That(result.PaidAt!.Value.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(handler.Calls, Is.Zero);
        });
    }

    [TestCase("\"amount\":12500", "\"amount\":12501")]
    [TestCase("\"orderCode\":1000", "\"orderCode\":1001")]
    [TestCase("a5532507", "b5532507")]
    [TestCase("\"code\":\"00\",\"success\":true", "\"code\":\"99\",\"success\":true")]
    [TestCase("\"success\":true", "\"success\":false")]
    public async Task UT_PAYOS_TamperedOrNonSuccessWebhookRejectedWithoutHttp(string original, string replacement)
    {
        var result = await gateway.VerifyAndNormalizeCallbackAsync(new(Webhook().Replace(original, replacement, StringComparison.Ordinal)), default);
        Assert.That(result.Status, Is.EqualTo(PaymentGatewayCallbackOutcome.Unverified));
        Assert.That(handler.Calls, Is.Zero);
    }

    [TestCase("not-json")]
    [TestCase("{}")]
    [TestCase("{\"data\":{\"amount\":1,\"amount\":2},\"signature\":\"fixture\"}")]
    public async Task UT_PAYOS_MalformedAndDuplicateFieldPayloadRejected(string json)
    {
        var result = await gateway.VerifyAndNormalizeCallbackAsync(new(json), default);
        Assert.That(result.Status, Is.EqualTo(PaymentGatewayCallbackOutcome.Unverified));
    }

    [Test]
    public async Task UT_PAYOS_CanonicalNullArrayAndBooleanFieldsAreAllSigned()
    {
        var data = new Dictionary<string, object?>
        {
            ["orderCode"] = 1000L, ["amount"] = 12500L, ["code"] = "00", ["currency"] = "VND",
            ["reference"] = "TF900001", ["transactionDateTime"] = "2026-10-07 17:02:03",
            ["extraNull"] = null, ["extraStringNull"] = "null", ["extraBool"] = true,
            ["extraArray"] = new[] { new { z = 2, a = "Việt" } }
        };
        // Manually specified expected canonical bytes, independent of the production serializer.
        const string canonical = "amount=12500&code=00&currency=VND&extraArray=[{\"a\":\"Việt\",\"z\":2}]&extraBool=true&extraNull=&extraStringNull=&orderCode=1000&reference=TF900001&transactionDateTime=2026-10-07 17:02:03";
        var payload = JsonSerializer.Serialize(new { code = "00", success = true, data, signature = Sign(canonical) });
        var result = await gateway.VerifyAndNormalizeCallbackAsync(new(payload), default);
        Assert.That(result.Status, Is.EqualTo(PaymentGatewayCallbackOutcome.Success));
        var altered = payload.Replace("\"z\":2", "\"z\":3", StringComparison.Ordinal);
        Assert.That((await gateway.VerifyAndNormalizeCallbackAsync(new(altered), default)).Status, Is.EqualTo(PaymentGatewayCallbackOutcome.Unverified));
    }

    [TestCase("amount", "12500.50", "12500.5")]
    [TestCase("amount", "0", "0")]
    [TestCase("orderCode", "0", "0")]
    [TestCase("currency", "\"USD\"", "USD")]
    [TestCase("code", "\"99\"", "99")]
    [TestCase("reference", "\"\"", "")]
    [TestCase("reference", "\"unsafe reference\"", "unsafe reference")]
    [TestCase("transactionDateTime", "\"2026-02-30 17:02:03\"", "2026-02-30 17:02:03")]
    [TestCase("transactionDateTime", "\"2026-10-07T17:02:03Z\"", "2026-10-07T17:02:03Z")]
    public async Task UT_PAYOS_SignedButInvalidFinancialFieldsAreRejected(string field, string raw, string canonicalValue)
    {
        // Deliberately sign invalid fields, so failure proves validation after signature verification.
        var fields = new Dictionary<string, (string Raw, string Canonical)>
        {
            ["amount"] = ("12500", "12500"), ["code"] = ("\"00\"", "00"),
            ["currency"] = ("\"VND\"", "VND"), ["orderCode"] = ("1000", "1000"),
            ["reference"] = ("\"TF900001\"", "TF900001"),
            ["transactionDateTime"] = ("\"2026-10-07 17:02:03\"", "2026-10-07 17:02:03")
        };
        fields[field] = (raw, canonicalValue);
        var canonical = string.Join('&', fields.Select(p => p.Key + "=" + p.Value.Canonical));
        var data = "{" + string.Join(',', fields.Select(p => "\"" + p.Key + "\":" + p.Value.Raw)) + "}";
        var payload = "{\"code\":\"00\",\"success\":true,\"data\":" + data + ",\"signature\":\"" + Sign(canonical) + "\"}";
        var result = await gateway.VerifyAndNormalizeCallbackAsync(new(payload), default);
        Assert.That(result.Status, Is.EqualTo(PaymentGatewayCallbackOutcome.Unverified));
        Assert.That(result.Amount, Is.Null);
        Assert.That(result.PaidAt, Is.Null);
        Assert.That(handler.Calls, Is.Zero);
    }

    [Test]
    public async Task UT_PAYOS_ArrayNumericTokensAreNormalizedWithoutChangingNestedObjectOrder()
    {
        const string data = "{\"reference\":\"TF900001\",\"orderCode\":1000,\"amount\":12500,\"currency\":\"VND\",\"code\":\"00\",\"transactionDateTime\":\"2026-10-07 17:02:03\",\"extra\":[{\"z\":2.0,\"a\":{\"z\":3.0,\"a\":null}}],\"description\":\"different display text\"}";
        const string canonical = "amount=12500&code=00&currency=VND&description=different display text&extra=[{\"a\":{\"z\":3,\"a\":null},\"z\":2}]&orderCode=1000&reference=TF900001&transactionDateTime=2026-10-07 17:02:03";
        var result = await gateway.VerifyAndNormalizeCallbackAsync(new("{\"code\":\"00\",\"success\":true,\"data\":" + data + ",\"signature\":\"" + Sign(canonical) + "\"}"), default);
        Assert.That(result.Status, Is.EqualTo(PaymentGatewayCallbackOutcome.Success));
        Assert.That(result.TransactionCode, Is.EqualTo("TF900001"));
        Assert.That(handler.Calls, Is.Zero);
    }

    [TestCase(2048, PaymentGatewayCreationOutcome.SessionCreated)]
    [TestCase(2049, PaymentGatewayCreationOutcome.Unknown)]
    public async Task UT_PAYOS_CheckoutUrlLengthIsBoundedBeforePersistence(int length, PaymentGatewayCreationOutcome outcome)
    {
        const string prefix = "https://pay.payos.vn/";
        handler.Data["checkoutUrl"] = prefix + new string('a', length - prefix.Length);
        var result = await gateway.CreatePaymentAsync(Request(), default);
        Assert.That(result.Outcome, Is.EqualTo(outcome));
        if (outcome == PaymentGatewayCreationOutcome.SessionCreated)
            Assert.That(result.Session!.PaymentUrl.Length, Is.EqualTo(2048));
        else Assert.That(result.Session, Is.Null);
        Assert.That(handler.Calls, Is.EqualTo(1));
    }

    [Test]
    public async Task UT_PAYOS_ExpiryAtCurrentTimeIsNotUsable()
    {
        handler.Data["expiredAt"] = Now.ToUnixTimeSeconds();
        var result = await gateway.CreatePaymentAsync(Request(), default);
        Assert.That(result.Outcome, Is.EqualTo(PaymentGatewayCreationOutcome.Unknown));
        Assert.That(result.Session, Is.Null);
    }

    [Test]
    public async Task UT_PAYOS_OversizedWebhookIsRejectedWithoutHttp()
    {
        var result = await gateway.VerifyAndNormalizeCallbackAsync(new(Webhook() + new string(' ', 65_536)), default);
        Assert.That(result.Status, Is.EqualTo(PaymentGatewayCallbackOutcome.Unverified));
        Assert.That(handler.Calls, Is.Zero);
    }

    private static PaymentGatewayRequest Request() => new(Guid.Parse("11111111-1111-1111-1111-111111111111"), 12500,
        "https://attacker.invalid/never-used", "not-required-by-payos", Now, 1000);
    private static string Webhook() => "{\"code\":\"00\",\"success\":true,\"data\":{\"reference\":\"TF900001\",\"orderCode\":1000,\"amount\":12500,\"currency\":\"VND\",\"code\":\"00\",\"transactionDateTime\":\"2026-10-07 17:02:03\"},\"signature\":\"" + CallbackVector + "\"}";
    private static string Sign(string text) => Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(FixtureKey), Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    private sealed class Clock : IClock
    {
        public DateTimeOffset UtcNow => Now;
        public TimeZoneInfo BusinessTimeZone => TimeZoneInfo.Utc;
        public DateTimeOffset ToBusinessTime(DateTimeOffset timestamp) => timestamp.ToOffset(TimeSpan.FromHours(7));
    }
    private sealed class Handler : HttpMessageHandler
    {
        public int Calls; public string? Body; public HttpMethod? Method; public string? Url; public string? ClientId; public string? ApiKey;
        public string Scenario = "valid";
        public Dictionary<string, object?> Data = new()
        {
            ["orderCode"] = 1000L, ["amount"] = 12500L, ["currency"] = "VND", ["status"] = "PENDING",
            ["checkoutUrl"] = "https://pay.payos.vn/web/fixture"
        };
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++; Method = request.Method; Url = request.RequestUri!.AbsoluteUri;
            ClientId = request.Headers.GetValues("x-client-id").Single(); ApiKey = request.Headers.GetValues("x-api-key").Single();
            Body = await request.Content!.ReadAsStringAsync(token);
            if (Scenario == "transport-error") throw new HttpRequestException("fixture transport");
            if (Scenario == "timeout") throw new TaskCanceledException("fixture timeout");
            var canonical = string.Join('&', Data.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => x.Key + "=" + Convert.ToString(x.Value, CultureInfo.InvariantCulture)));
            var response = Scenario == "invalid-json" ? "not-json" : JsonSerializer.Serialize(new
            {
                code = Scenario == "provider-error" ? "99" : "00", data = Data,
                signature = Scenario == "invalid-signature" ? new string('0', 64) : Sign(canonical)
            });
            return new(Scenario == "http-error" ? HttpStatusCode.InternalServerError : HttpStatusCode.OK) { Content = new StringContent(response, Encoding.UTF8, "application/json") };
        }
    }
}
