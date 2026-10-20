using System.Text;
using Frms.Api.Validation;

namespace Frms.ApiTests;

[TestFixture]
public sealed class PasswordValidationBulkEdgeTests
{
    public static IEnumerable<TestCaseData> PasswordCases()
    {
        for (var length = 0; length <= 70; length++)
        {
            var value = new string('A', length);
            yield return Case(value, $"ascii_length_{length}");
        }

        for (var count = 1; count <= 40; count++)
            yield return Case(new string('ắ', count), $"vietnamese_multibyte_count_{count}");

        for (var count = 1; count <= 30; count++)
            yield return Case(string.Concat(Enumerable.Repeat("😀", count)), $"emoji_count_{count}");

        for (var count = 1; count <= 59; count++)
        {
            var value = new string('A', count) + new string('ắ', 60 - count);
            yield return Case(value, $"mixed_ascii_vietnamese_{count}");
        }
    }

    [TestCaseSource(nameof(PasswordCases))]
    public void IsValid_BulkLengthAndUtf8Boundaries_MatchesContract(string password, bool expected)
    {
        Assert.That(new FrmsPasswordAttribute().IsValid(password), Is.EqualTo(expected));
    }

    private static TestCaseData Case(string password, string name)
    {
        var expected = password.Length is >= 8 and <= 64 && Encoding.UTF8.GetByteCount(password) <= 72;
        return new TestCaseData(password, expected).SetName(name);
    }
}
