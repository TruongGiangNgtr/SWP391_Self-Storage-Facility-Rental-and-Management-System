using Frms.Business.Exceptions;
using Frms.Business.Models.Commands;
using Frms.Business.Validators;

namespace Frms.UnitTests;

[TestFixture]
public sealed class CustomerProfileValidatorEdgeCaseTests
{
    public static IEnumerable<TestCaseData> InvalidFullNames()
    {
        yield return new TestCaseData((object?)null).SetName("null_name");
        yield return new TestCaseData("").SetName("empty_name");
        yield return new TestCaseData(" ").SetName("single_space_name");
        yield return new TestCaseData("\t\r\n").SetName("whitespace_only_name");
        yield return new TestCaseData(new string('N', 151)).SetName("name_151_chars");
        yield return new TestCaseData(new string('N', 2000)).SetName("name_extremely_long");
    }

    [TestCaseSource(nameof(InvalidFullNames))]
    public void ValidateAndNormalize_InvalidUpdatedName_IsRejected(string? name) =>
        AssertValidationError(new(true, name, false, null, false, null));

    public static IEnumerable<TestCaseData> ValidFullNames()
    {
        yield return new TestCaseData("A").SetName("one_character_name");
        yield return new TestCaseData(new string('N', 150)).SetName("name_at_maximum_length");
        yield return new TestCaseData("  Nguyen Van A  ").SetName("trims_name_edges");
        yield return new TestCaseData("Nguyen  Van  A").SetName("preserves_internal_spaces");
        yield return new TestCaseData("Nguyễn Văn Á").SetName("unicode_vietnamese_name");
        yield return new TestCaseData("O'Connor-Smith").SetName("punctuation_in_name");
    }

    [TestCaseSource(nameof(ValidFullNames))]
    public void ValidateAndNormalize_ValidUpdatedName_ReturnsTrimmedName(string name)
    {
        var result = CustomerProfileValidator.ValidateAndNormalize(new(true, name, false, null, false, null));
        Assert.That(result.FullName, Is.EqualTo(name.Trim()));
    }

    public static IEnumerable<TestCaseData> InvalidAddresses()
    {
        yield return new TestCaseData(new string('A', 1001)).SetName("address_1001_chars");
        yield return new TestCaseData(new string('A', 5000)).SetName("address_extremely_long");
    }

    [TestCaseSource(nameof(InvalidAddresses))]
    public void ValidateAndNormalize_AddressOverLimit_IsRejected(string address) =>
        AssertValidationError(new(false, null, true, address, false, null));

    public static IEnumerable<TestCaseData> ValidAddresses()
    {
        yield return new TestCaseData("A").SetName("address_one_character");
        yield return new TestCaseData(new string('A', 1000)).SetName("address_at_maximum_length");
        yield return new TestCaseData("  Ho Chi Minh City  ").SetName("address_is_trimmed");
        yield return new TestCaseData("").SetName("empty_address_becomes_null");
        yield return new TestCaseData("   ").SetName("whitespace_address_becomes_null");
        yield return new TestCaseData("Đường Nguyễn Huệ, Quận 1").SetName("unicode_address");
    }

    [TestCaseSource(nameof(ValidAddresses))]
    public void ValidateAndNormalize_ValidAddress_RespectsTrimAndEmptyRules(string address)
    {
        var result = CustomerProfileValidator.ValidateAndNormalize(new(false, null, true, address, false, null));
        var expected = string.IsNullOrEmpty(address.Trim()) ? null : address.Trim();
        Assert.That(result.Address, Is.EqualTo(expected));
    }

    public static IEnumerable<TestCaseData> InvalidCccds()
    {
        yield return new TestCaseData(new string('1', 21)).SetName("cccd_21_chars");
        yield return new TestCaseData(new string('1', 1000)).SetName("cccd_extremely_long");
    }

    [TestCaseSource(nameof(InvalidCccds))]
    public void ValidateAndNormalize_CccdOverLimit_IsRejected(string cccd) =>
        AssertValidationError(new(false, null, false, null, true, cccd));

    public static IEnumerable<TestCaseData> ValidCccds()
    {
        yield return new TestCaseData("1").SetName("cccd_one_character");
        yield return new TestCaseData(new string('1', 20)).SetName("cccd_at_maximum_length");
        yield return new TestCaseData("  012345678901  ").SetName("cccd_is_trimmed");
        yield return new TestCaseData("").SetName("empty_cccd_becomes_null");
        yield return new TestCaseData("   ").SetName("whitespace_cccd_becomes_null");
        yield return new TestCaseData((string?)null).SetName("explicit_null_cccd_is_allowed");
    }

    [TestCaseSource(nameof(ValidCccds))]
    public void ValidateAndNormalize_ValidCccd_RespectsTrimAndClearRules(string? cccd)
    {
        var result = CustomerProfileValidator.ValidateAndNormalize(new(false, null, false, null, true, cccd));
        var expected = string.IsNullOrEmpty(cccd?.Trim()) ? null : cccd!.Trim();
        Assert.That(result.Cccd, Is.EqualTo(expected));
        Assert.That(result.UpdateCccd, Is.True);
    }

    [TestCase(false, false, false)]
    public void ValidateAndNormalize_NoFieldsSelected_IsRejected(bool updateName, bool updateAddress, bool updateCccd) =>
        AssertValidationError(new(updateName, null, updateAddress, null, updateCccd, null));

    [Test]
    public void ValidateAndNormalize_InvalidNameTakesPrecedenceOverInvalidAddressAndCccd()
    {
        var error = Assert.Throws<BusinessException>(() => CustomerProfileValidator.ValidateAndNormalize(
            new(true, " ", true, new string('A', 1001), true, new string('1', 21))));
        Assert.That(error!.Code, Is.EqualTo("VALIDATION_ERROR"));
    }

    [Test]
    public void ValidateAndNormalize_FieldsNotSelectedAreStillTrimmedButNotValidatedForRequiredName()
    {
        var result = CustomerProfileValidator.ValidateAndNormalize(new(false, null, true, "  Address  ", true, "  123  "));
        Assert.Multiple(() =>
        {
            Assert.That(result.FullName, Is.Null);
            Assert.That(result.Address, Is.EqualTo("Address"));
            Assert.That(result.Cccd, Is.EqualTo("123"));
        });
    }

    private static void AssertValidationError(UpdateCustomerProfileCommand command)
    {
        var error = Assert.Throws<BusinessException>(() => CustomerProfileValidator.ValidateAndNormalize(command));
        Assert.Multiple(() =>
        {
            Assert.That(error!.Code, Is.EqualTo("VALIDATION_ERROR"));
            Assert.That(error.SuggestedStatusCode, Is.EqualTo(400));
        });
    }
}
