using Frms.Api.Validation;

namespace Frms.ApiTests;

[TestFixture]
public sealed class PasswordValidationEdgeCaseTests
{
    public static IEnumerable<TestCaseData> CharacterLengthCases()
    {
        foreach (var length in new[] { 0, 1, 2, 7 })
            yield return new TestCaseData(new string('a', length), false).SetName($"ascii_password_length_{length}_is_too_short");
        foreach (var length in new[] { 8, 9, 16, 32, 63, 64 })
            yield return new TestCaseData(new string('a', length), true).SetName($"ascii_password_length_{length}_is_allowed");
        foreach (var length in new[] { 65, 66, 72, 100, 1000 })
            yield return new TestCaseData(new string('a', length), false).SetName($"ascii_password_length_{length}_is_too_long");
    }

    [TestCaseSource(nameof(CharacterLengthCases))]
    public void IsValid_CharacterLengthEdges_AreEnforced(string password, bool expected) =>
        Assert.That(new FrmsPasswordAttribute().IsValid(password), Is.EqualTo(expected));

    public static IEnumerable<TestCaseData> Utf8ByteCases()
    {
        yield return new TestCaseData(new string('é', 36), true).SetName("two_byte_characters_exactly_72_utf8_bytes");
        yield return new TestCaseData(new string('é', 37), false).SetName("two_byte_characters_over_72_utf8_bytes");
        yield return new TestCaseData(new string('ắ', 24), true).SetName("three_byte_characters_exactly_72_utf8_bytes");
        yield return new TestCaseData(new string('ắ', 25), false).SetName("three_byte_characters_over_72_utf8_bytes");
        yield return new TestCaseData(new string('a', 8) + new string('é', 32), true).SetName("mixed_ascii_and_two_byte_exactly_72_bytes");
        yield return new TestCaseData(new string('a', 8) + new string('é', 33), false).SetName("mixed_ascii_and_two_byte_over_72_bytes");
        yield return new TestCaseData(string.Concat(Enumerable.Repeat("😀", 18)), true).SetName("emoji_password_exactly_72_utf8_bytes");
        yield return new TestCaseData(string.Concat(Enumerable.Repeat("😀", 19)), false).SetName("emoji_password_over_72_utf8_bytes");
        yield return new TestCaseData("Passw0rd😀", true).SetName("mixed_ascii_and_emoji_within_byte_limit");
        yield return new TestCaseData("ắbcdefg😀", true).SetName("unicode_password_minimum_character_boundary");
        yield return new TestCaseData(new string('é', 4), false).SetName("unicode_password_too_few_characters");
        yield return new TestCaseData(string.Concat(Enumerable.Repeat("😀", 33)), false).SetName("emoji_password_exceeds_character_limit");
    }

    [TestCaseSource(nameof(Utf8ByteCases))]
    public void IsValid_Utf8ByteBoundaries_AreEnforced(string password, bool expected) =>
        Assert.That(new FrmsPasswordAttribute().IsValid(password), Is.EqualTo(expected));

    public static IEnumerable<TestCaseData> NonStringValues()
    {
        yield return new TestCaseData((object?)null).SetName("null_value_is_rejected");
        yield return new TestCaseData(12345678).SetName("integer_value_is_rejected");
        yield return new TestCaseData(true).SetName("boolean_value_is_rejected");
        yield return new TestCaseData((object)new object[] { "not", "a", "string" }).SetName("object_array_value_is_rejected");
    }

    [TestCaseSource(nameof(NonStringValues))]
    public void IsValid_NonStringValues_AreRejected(object? value) =>
        Assert.That(new FrmsPasswordAttribute().IsValid(value), Is.False);

    [Test]
    public void FormatErrorMessage_ExplainsCharacterAndByteLimits()
    {
        var message = new FrmsPasswordAttribute().FormatErrorMessage("Password");
        Assert.That(message, Is.EqualTo("Password must contain 8 to 64 characters and no more than 72 UTF-8 bytes."));
    }
}
