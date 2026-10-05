using Frms.Api.Validation;
namespace Frms.ApiTests;

[TestFixture]
public sealed class PasswordValidationTests
{
    [TestCase("1234567", false)]
    [TestCase("12345678", true)]
    [TestCase("1234567890123456789012345678901234567890123456789012345678901234", true)]
    public void IsValid_LengthBoundaries_EnforcesEightToSixtyFourCharacters(string password, bool expected) => Assert.That(new FrmsPasswordAttribute().IsValid(password), Is.EqualTo(expected));
    [Test]
    public void IsValid_MultibytePasswordOverSeventyTwoBytes_ReturnsFalse() => Assert.That(new FrmsPasswordAttribute().IsValid(new string('ắ', 30)), Is.False);
}
