using System.Web;
using Frms.Business.Abstractions.External;
using Frms.Business.Exceptions;
using Frms.Infrastructure.Payments;
using Microsoft.Extensions.Options;

namespace Frms.UnitTests;

[TestFixture, Category("VnPayGateway")]
public sealed class VnPayPaymentGatewayTests
{
    private static readonly Guid PaymentId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 7, 10, 0, 0, TimeSpan.Zero);
    private const string ReturnUrl = "https://app.example.test/payment/vnpay-return";
    private const string ExpectedHash = "71ab75f7b20de5ab143ae184be0dd972aa9ef6a575a2cbac40ab7f811c811719edbe363e1d741f963dcb4fde6e25323c5cb26ffc14c436b4bf8e6753a851a2bb";
    private const string ValidCallback = "?vnp_Amount=1250000&vnp_BankCode=NCB&vnp_BankTranNo=VNP123&vnp_CardType=ATM&vnp_OrderInfo=display+changed&vnp_PayDate=20261007170506&vnp_ResponseCode=00&vnp_TmnCode=TESTCODE&vnp_TransactionNo=987654321&vnp_TransactionStatus=00&vnp_TxnRef=11111111222233334444555555555555&vnp_SecureHash=9dd3934a5b13e833a3c6a1bce76c787225ccc2b0dcbde45529ce624d1dd9eb870a3b64667ec282e6185a1a703688e5a2bb514590a5a5fc98a51b85051d1761fd";

    [Test]
    public async Task CreatePayment_UsesCanonicalV21FieldsAndIndependentHmacVector()
    {
        var gateway = Gateway();

        var result = await gateway.CreatePaymentAsync(new PaymentGatewayRequest(
            PaymentId, 12500m, ReturnUrl, "203.0.113.10", CreatedAt), CancellationToken.None);

        var session = result.Session!;
        var uri = new Uri(session.PaymentUrl);
        var query = HttpUtility.ParseQueryString(uri.Query);
        Assert.Multiple(() =>
        {
            Assert.That(uri.GetLeftPart(UriPartial.Path), Is.EqualTo("https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"));
            Assert.That(query["vnp_Version"], Is.EqualTo("2.1.0"));
            Assert.That(query["vnp_Command"], Is.EqualTo("pay"));
            Assert.That(query["vnp_Amount"], Is.EqualTo("1250000"));
            Assert.That(query["vnp_CreateDate"], Is.EqualTo("20261007170000"));
            Assert.That(query["vnp_ExpireDate"], Is.EqualTo("20261007171500"));
            Assert.That(query["vnp_TxnRef"], Is.EqualTo(PaymentId.ToString("N")));
            Assert.That(query["vnp_SecureHash"], Is.EqualTo(ExpectedHash));
            Assert.That(query.AllKeys, Does.Not.Contain("vnp_IpnUrl"));
            Assert.That(session.ExpiresAt, Is.EqualTo(CreatedAt.AddMinutes(15)));
            Assert.That(session.TransactionCode, Is.Null);
        });
    }

    [Test]
    public async Task Callback_ValidSuccessNormalizesAmountReferenceAndVietnamTimeToUtc()
    {
        var result = await Gateway().VerifyAndNormalizeCallbackAsync(
            new PaymentGatewayCallbackRequest(ValidCallback), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(PaymentGatewayCallbackOutcome.Success));
            Assert.That(result.PaymentId, Is.EqualTo(PaymentId));
            Assert.That(result.Amount, Is.EqualTo(12500m));
            Assert.That(result.TransactionCode, Is.EqualTo("987654321"));
            Assert.That(result.PaidAt, Is.EqualTo(new DateTimeOffset(2026, 10, 7, 10, 5, 6, TimeSpan.Zero)));
        });
    }

    [TestCase("vnp_Amount=1250000", "vnp_Amount=1250100")]
    [TestCase("vnp_TmnCode=TESTCODE", "vnp_TmnCode=OTHER")]
    [TestCase("vnp_ResponseCode=00", "vnp_ResponseCode=24")]
    public async Task Callback_AnyUnsignedTamperingIsRejected(string original, string changed)
    {
        var result = await Gateway().VerifyAndNormalizeCallbackAsync(
            new PaymentGatewayCallbackRequest(ValidCallback.Replace(original, changed, StringComparison.Ordinal)),
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(PaymentGatewayCallbackOutcome.Unverified));
    }

    [Test]
    public async Task Callback_AuthenticatedFailureNormalizesToFailedWithoutPaidAt()
    {
        var payload = SignForTest(new Dictionary<string, string>
        {
            ["vnp_Amount"] = "1250000",
            ["vnp_ResponseCode"] = "24",
            ["vnp_TmnCode"] = "TESTCODE",
            ["vnp_TransactionNo"] = "987654321",
            ["vnp_TransactionStatus"] = "02",
            ["vnp_TxnRef"] = PaymentId.ToString("N")
        });

        var result = await Gateway().VerifyAndNormalizeCallbackAsync(new(payload), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(PaymentGatewayCallbackOutcome.Failed));
        Assert.That(result.PaidAt, Is.Null);
    }

    [TestCase("merchant")]
    [TestCase("reference")]
    [TestCase("timestamp")]
    [TestCase("transaction")]
    public async Task Callback_ValidSignatureButInvalidRequiredIdentityOrSuccessDataRemainsUnknown(string invalidField)
    {
        var fields = SuccessFields();
        if (invalidField == "merchant") fields["vnp_TmnCode"] = "OTHER";
        if (invalidField == "reference") fields["vnp_TxnRef"] = "not-a-payment-id";
        if (invalidField == "timestamp") fields["vnp_PayDate"] = "invalid";
        if (invalidField == "transaction") fields.Remove("vnp_TransactionNo");

        var result = await Gateway().VerifyAndNormalizeCallbackAsync(
            new PaymentGatewayCallbackRequest(SignForTest(fields)), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(PaymentGatewayCallbackOutcome.Unknown));
            Assert.That(result.PaymentId, Is.EqualTo(Guid.Empty));
            Assert.That(result.TransactionCode, Is.Null);
        });
    }

    [Test]
    public async Task Callback_ResponseCodeAloneCannotMarkSuccess()
    {
        var fields = SuccessFields();
        fields["vnp_TransactionStatus"] = "02";

        var result = await Gateway().VerifyAndNormalizeCallbackAsync(
            new PaymentGatewayCallbackRequest(SignForTest(fields)), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(PaymentGatewayCallbackOutcome.Failed));
        Assert.That(result.PaidAt, Is.Null);
    }

    [TestCase("999.999", "PAYMENT_AMOUNT_UNSUPPORTED")]
    [TestCase("0", "PAYMENT_AMOUNT_UNSUPPORTED")]
    public void Preflight_RejectsAmountsThatCannotBeRepresentedByVnPay(string amount, string code)
    {
        var error = Assert.Throws<BusinessException>(() => Gateway().ValidatePaymentRequest(new(
            decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), ReturnUrl, "203.0.113.10")));

        Assert.That(error!.Code, Is.EqualTo(code));
    }

    [TestCase("https://evil.example/return", "203.0.113.10")]
    [TestCase(ReturnUrl, "not-an-ip")]
    public void Preflight_RejectsMismatchedReturnUrlOrInvalidClientIp(string returnUrl, string ip)
    {
        var error = Assert.Throws<BusinessException>(() =>
            Gateway().ValidatePaymentRequest(new(12500m, returnUrl, ip)));

        Assert.That(error!.Code, Is.EqualTo("VALIDATION_ERROR"));
    }

    [Test]
    public void SensitiveConfigurationAndPayloadAreNotRenderedByToString()
    {
        var options = Options();
        Assert.That(options.ToString(), Does.Not.Contain(options.HashSecret).And.Not.Contain(options.TmnCode));
        Assert.That(new PaymentGatewayCallbackRequest(ValidCallback).ToString(), Does.Not.Contain("vnp_SecureHash"));
    }

    private static VnPayPaymentGateway Gateway() => new(Microsoft.Extensions.Options.Options.Create(Options()));

    private static VnPayOptions Options() => new()
    {
        TmnCode = "TESTCODE",
        HashSecret = "fixture-hash-secret",
        CredentialSetId = "sandbox-fixture",
        PaymentUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
        ReturnUrl = ReturnUrl,
        IpnUrl = "https://example.test/api/v1/payments/vnpay/ipn",
        Locale = "vn",
        OrderType = "other",
        ExpiryMinutes = 15
    };

    private static string SignForTest(IReadOnlyDictionary<string, string> fields)
    {
        var signed = string.Join("&", fields.OrderBy(field => field.Key, StringComparer.Ordinal)
            .Select(field => $"{System.Net.WebUtility.UrlEncode(field.Key)}={System.Net.WebUtility.UrlEncode(field.Value)}"));
        using var hmac = new System.Security.Cryptography.HMACSHA512(
            System.Text.Encoding.UTF8.GetBytes("fixture-hash-secret"));
        var hash = Convert.ToHexStringLower(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signed)));
        return $"?{signed}&vnp_SecureHash={hash}";
    }

    private static Dictionary<string, string> SuccessFields() => new()
    {
        ["vnp_Amount"] = "1250000",
        ["vnp_PayDate"] = "20261007170506",
        ["vnp_ResponseCode"] = "00",
        ["vnp_TmnCode"] = "TESTCODE",
        ["vnp_TransactionNo"] = "987654321",
        ["vnp_TransactionStatus"] = "00",
        ["vnp_TxnRef"] = PaymentId.ToString("N")
    };
}
