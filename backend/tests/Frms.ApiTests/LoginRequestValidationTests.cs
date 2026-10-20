using System.ComponentModel.DataAnnotations;
using Frms.Api.DTOs.Requests;

namespace Frms.ApiTests;

[TestFixture]
public sealed class LoginRequestValidationTests
{
    [Test]
    public void CustomerLoginRequest_ValidPhoneAndPassword_PassesValidation()
    {
        var request = new CustomerLoginRequest { PhoneNumber = "+84901234567", Password = "correct-horse-battery-staple" };
        Assert.That(Validate(request), Is.Empty);
    }

    [Test]
    public void CustomerLoginRequest_PhoneLongerThanThirtyCharacters_FailsValidation()
    {
        var request = new CustomerLoginRequest { PhoneNumber = new string('1', 31), Password = "correct-horse-battery-staple" };
        Assert.That(Validate(request).Select(error => error.MemberNames.FirstOrDefault()), Does.Contain(nameof(CustomerLoginRequest.PhoneNumber)));
    }

    [Test]
    public void CustomerLoginRequest_PasswordBelowMinimumLength_FailsValidation()
    {
        var request = new CustomerLoginRequest { PhoneNumber = "+84901234567", Password = "short" };
        Assert.That(Validate(request).Select(error => error.MemberNames.FirstOrDefault()), Does.Contain(nameof(CustomerLoginRequest.Password)));
    }

    [Test]
    public void EmployeeLoginRequest_InvalidEmail_FailsValidation()
    {
        var request = new EmployeeLoginRequest { Email = "not-an-email", Password = "correct-horse-battery-staple" };
        Assert.That(Validate(request).Select(error => error.MemberNames.FirstOrDefault()), Does.Contain(nameof(EmployeeLoginRequest.Email)));
    }

    [Test]
    public void EmployeeLoginRequest_EmailLongerThanTwoHundredFiftyFourCharacters_FailsValidation()
    {
        var request = new EmployeeLoginRequest
        {
            Email = $"{new string('a', 245)}@example.test",
            Password = "correct-horse-battery-staple"
        };
        Assert.That(request.Email.Length, Is.GreaterThan(254));
        Assert.That(Validate(request).Select(error => error.MemberNames.FirstOrDefault()), Does.Contain(nameof(EmployeeLoginRequest.Email)));
    }

    private static IReadOnlyList<ValidationResult> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        return results;
    }
}
