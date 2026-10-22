using Frms.Business.Exceptions;
using Frms.Business.Models.Commands;
using Frms.Business.Validators;

namespace Frms.UnitTests;

[TestFixture, Category("CustomerProfileValidation")]
public sealed class CustomerProfileAdvancedBoundaryMatrixTests
{
    public static IEnumerable<TestCaseData> Cases()
    {
        for (var length = 1; length <= 40; length++)
            yield return Case(true, new string('N', length), false, null, false, null, true, $"name_valid_{length}");
        foreach (var length in new[] { 149, 150 })
            yield return Case(true, new string('N', length), false, null, false, null, true, $"name_max_boundary_{length}");
        for (var length = 151; length <= 160; length++)
            yield return Case(true, new string('N', length), false, null, false, null, false, $"name_over_limit_{length}");
        for (var length = 0; length <= 9; length++)
            yield return Case(false, null, true, new string('A', length), false, null, true, $"address_valid_{length}");
        foreach (var length in new[] { 999, 1000 })
            yield return Case(false, null, true, new string('A', length), false, null, true, $"address_max_boundary_{length}");
        for (var length = 1001; length <= 1010; length++)
            yield return Case(false, null, true, new string('A', length), false, null, false, $"address_over_limit_{length}");
        for (var length = 0; length <= 9; length++)
            yield return Case(false, null, false, null, true, new string('7', length), true, $"cccd_valid_{length}");
        foreach (var length in new[] { 19, 20 })
            yield return Case(false, null, false, null, true, new string('7', length), true, $"cccd_max_boundary_{length}");
        for (var length = 21; length <= 30; length++)
            yield return Case(false, null, false, null, true, new string('7', length), false, $"cccd_over_limit_{length}");
        yield return Case(false, null, false, null, false, null, false, "empty_patch_rejected");
        yield return Case(false, null, true, "  Address  ", false, null, true, "address_only_patch");
        yield return Case(false, null, false, null, true, " 12345678901234567890 ", true, "cccd_only_trimmed_at_limit");
        // Defect probe: a field not selected for update should normally be ignored by patch validation.
        yield return Case(false, null, false, new string('A', 1001), true, "123", true, "unselected_long_address_should_be_ignored");
    }

    [TestCaseSource(nameof(Cases))]
    public void ValidateAndNormalize_AdvancedBoundaryMatrix(
        bool updateName, string? name, bool updateAddress, string? address,
        bool updateCccd, string? cccd, bool expectedValid)
    {
        var command = new UpdateCustomerProfileCommand(updateName, name, updateAddress, address, updateCccd, cccd);
        if (!expectedValid)
        {
            var error = Assert.Throws<BusinessException>(() => CustomerProfileValidator.ValidateAndNormalize(command));
            Assert.That(error!.Code, Is.EqualTo("VALIDATION_ERROR"));
            return;
        }

        var result = CustomerProfileValidator.ValidateAndNormalize(command);
        if (updateName) Assert.That(result.FullName, Is.EqualTo(name?.Trim()));
        if (updateAddress) Assert.That(result.Address, Is.EqualTo(string.IsNullOrWhiteSpace(address) ? null : address!.Trim()));
        if (updateCccd) Assert.That(result.Cccd, Is.EqualTo(string.IsNullOrWhiteSpace(cccd) ? null : cccd!.Trim()));
    }

    private static TestCaseData Case(bool updateName, string? name, bool updateAddress, string? address,
        bool updateCccd, string? cccd, bool expectedValid, string testName) =>
        new TestCaseData(updateName, name, updateAddress, address, updateCccd, cccd, expectedValid).SetName(testName);
}
