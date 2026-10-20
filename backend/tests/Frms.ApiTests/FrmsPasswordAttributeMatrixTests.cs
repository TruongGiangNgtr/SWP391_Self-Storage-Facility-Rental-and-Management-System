using Frms.Api.Validation;

namespace Frms.ApiTests;

[TestFixture, Category("PasswordValidation")]
public sealed class FrmsPasswordAttributeMatrixTests
{
    public static IEnumerable<TestCaseData> PasswordCases()
    {
        for (var length = 8; length <= 28; length++)
            yield return Case(new string('a', length), true, $"ascii_valid_length_{length}");
        for (var length = 8; length <= 36; length++)
            yield return Case(new string('é', length), length <= 36, $"two_byte_valid_length_{length}");
        for (var length = 0; length < 8; length++)
            yield return Case(new string('a', length), false, $"ascii_too_short_{length}");
        for (var length = 65; length <= 79; length++)
            yield return Case(new string('a', length), false, $"ascii_too_long_{length}");
        for (var length = 25; length <= 51; length++)
            yield return Case(new string('ế', length), false, $"three_byte_over_limit_{length}");
    }

    [TestCaseSource(nameof(PasswordCases))]
    public void IsValid_PasswordBoundaryMatrixMatchesCharacterAndUtf8Limits(string password, bool expected)
    {
        Assert.That(new FrmsPasswordAttribute().IsValid(password), Is.EqualTo(expected));
    }

    private static TestCaseData Case(string password, bool expected, string name) => new TestCaseData(password, expected).SetName(name);
}
