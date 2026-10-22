using System.ComponentModel.DataAnnotations;
using Frms.Api.DTOs.Requests;

namespace Frms.ApiTests;

[TestFixture, Category("RequestValidation")]
public sealed class RegistrationRequestValidationMatrixTests
{
    public static IEnumerable<TestCaseData> Cases()
    {
        for (var length = 1; length <= 20; length++) yield return Case("FullName", new string('N', length), true, $"request_name_valid_{length}");
        for (var length = 151; length <= 155; length++) yield return Case("FullName", new string('N', length), false, $"request_name_too_long_{length}");
        for (var length = 1; length <= 20; length++) yield return Case("PhoneNumber", new string('9', length), true, $"request_phone_valid_{length}");
        for (var length = 31; length <= 41; length++) yield return Case("PhoneNumber", new string('9', length), false, $"request_phone_too_long_{length}");
        for (var i = 0; i < 10; i++) yield return Case("Email", $"user{i}@example.test", true, $"request_email_valid_{i}");
        var invalidEmails = new[] { "", " ", "no-at-sign", "@example.test", "user@", "user@@example.test", "user@example@@test", "missing-domain@", "@@", "a b@example.test" };
        for (var i = 0; i < invalidEmails.Length; i++) yield return Case("Email", invalidEmails[i], false, $"request_email_invalid_{i}");
        for (var length = 8; length <= 17; length++) yield return Case("Password", new string('a', length), true, $"request_password_valid_{length}");
        for (var length = 0; length <= 7; length++) yield return Case("Password", new string('a', length), false, $"request_password_short_{length}");
        for (var length = 65; length <= 66; length++) yield return Case("Password", new string('a', length), false, $"request_password_long_{length}");
        yield return Case("Address", new string('A', 1000), true, "request_address_max_boundary_1000");
        yield return Case("Address", new string('A', 1001), false, "request_address_over_limit_1001");
        yield return Case("Cccd", new string('7', 20), true, "request_cccd_max_boundary_20");
        yield return Case("Cccd", new string('7', 21), false, "request_cccd_over_limit_21");
    }

    [TestCaseSource(nameof(Cases))]
    public void RegisterCustomerRequest_DataAnnotationsMatchDeclaredContract(string field, string value, bool expectedValid)
    {
        var request = new RegisterCustomerRequest
        {
            FullName = field == "FullName" ? value : "Khoa",
            PhoneNumber = field == "PhoneNumber" ? value : "0900000000",
            Email = field == "Email" ? value : "khoa@example.test",
            Password = field == "Password" ? value : "ValidPassword!",
            Address = field == "Address" ? value : "HCM",
            Cccd = field == "Cccd" ? value : "001234567890"
        };
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        Assert.That(valid, Is.EqualTo(expectedValid), string.Join("; ", results.Select(x => x.ErrorMessage)));
    }

    private static TestCaseData Case(string field, string value, bool expectedValid, string name) =>
        new TestCaseData(field, value, expectedValid).SetName(name);
}
