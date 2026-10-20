using Frms.Business.Exceptions;
using Frms.Business.Models.Commands;
using Frms.Business.Validators;

namespace Frms.UnitTests;

[TestFixture, Category("CustomerProfileValidation")]
public sealed class CustomerProfileValidatorMatrixTests
{
    public static IEnumerable<TestCaseData> ProfileCases()
    {
        for (var length = 0; length < 30; length++)
            yield return Case(new string(' ', length), false, null, false, null, $"blank_name_{length}");
        for (var length = 151; length <= 180; length++)
            yield return Case(new string('N', length), false, null, false, null, $"long_name_{length}");
        for (var length = 1; length <= 25; length++)
            yield return Case(new string('N', length), false, null, false, null, $"valid_name_{length}", true);
        for (var length = 1001; length <= 1025; length++)
            yield return Case("Khoa", true, new string('A', length), false, null, $"long_address_{length}");
        for (var length = 21; length <= 40; length++)
            yield return Case("Khoa", false, null, true, new string('7', length), $"long_cccd_{length}");
        for (var length = 0; length < 20; length++)
        {
            var address = length == 0 ? null : new string(' ', length);
            yield return Case("  Khoa  ", true, address, true, "  ", $"normalization_blank_optional_{length}", true);
        }
    }

    [TestCaseSource(nameof(ProfileCases))]
    public void ValidateAndNormalize_ProfileMatrixHonorsLengthAndNormalizationRules(
        string? name, bool updateAddress, string? address, bool updateCccd, string? cccd, bool expectedValid)
    {
        var command = new UpdateCustomerProfileCommand(true, name, updateAddress, address, updateCccd, cccd);
        if (!expectedValid)
        {
            var error = Assert.Throws<BusinessException>(() => CustomerProfileValidator.ValidateAndNormalize(command));
            Assert.That(error!.Code, Is.EqualTo("VALIDATION_ERROR"));
            return;
        }

        var result = CustomerProfileValidator.ValidateAndNormalize(command);
        Assert.That(result.FullName, Is.EqualTo(name?.Trim()));
        if (address is null || string.IsNullOrWhiteSpace(address)) Assert.That(result.Address, Is.Null);
        if (cccd is null || string.IsNullOrWhiteSpace(cccd)) Assert.That(result.Cccd, Is.Null);
    }

    private static TestCaseData Case(string? name, bool updateAddress, string? address, bool updateCccd,
        string? cccd, string caseName, bool expectedValid = false) =>
        new TestCaseData(name, updateAddress, address, updateCccd, cccd, expectedValid).SetName(caseName);
}
