using Frms.Business.Exceptions;
using Frms.Business.Models.Commands;
using Frms.Business.Validators;

namespace Frms.UnitTests;

[TestFixture]
public sealed class CustomerProfileValidatorTests
{
    [Test]
    public void EmptyPatch_IsRejected() =>
        Assert.Throws<BusinessException>(() =>
            CustomerProfileValidator.ValidateAndNormalize(new(false, null, false, null, false, null)));

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public void NullOrBlankName_IsRejected(string? name) =>
        Assert.Throws<BusinessException>(() =>
            CustomerProfileValidator.ValidateAndNormalize(new(true, name, false, null, false, null)));

    [Test]
    public void NormalizesAndAllowsExplicitNullToClearCccd()
    {
        var result = CustomerProfileValidator.ValidateAndNormalize(
            new(true, "  Name  ", true, " ", true, null));
        Assert.Multiple(() =>
        {
            Assert.That(result.FullName, Is.EqualTo("Name"));
            Assert.That(result.Address, Is.Null);
            Assert.That(result.Cccd, Is.Null);
            Assert.That(result.UpdateCccd, Is.True);
        });
    }

    [Test]
    public void RejectsTooLongFields() =>
        Assert.Throws<BusinessException>(() =>
            CustomerProfileValidator.ValidateAndNormalize(new(true, "Name", false, null, true, new string('7', 21))));
}
