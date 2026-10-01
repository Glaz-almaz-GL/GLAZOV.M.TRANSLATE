using GLAZOV.M.TRANSLATE.Domain.Linguistics.Languages.Codes;

namespace GLAZOV.M.TRANSLATE.Domain.Tests.Linguistics.Codes;

/// <summary>
/// Verifies normalization and validation of the ISO 639 language codes.
/// </summary>
public sealed class LanguageCodeTests
{
    [Theory]
    [InlineData("ru", "ru")]
    [InlineData("RU", "ru")]
    [InlineData("  Ru  ", "ru")]
    public void Iso6391Code_NormalizesToLowerCase(string value, string expected)
    {
        Assert.Equal(expected, new Iso6391Code(value).Value);
    }

    [Theory]
    [InlineData("r")]
    [InlineData("rus")]
    public void Iso6391Code_WrongLength_ThrowsArgumentException(string value)
    {
        Assert.Throws<ArgumentException>(() => new Iso6391Code(value));
    }

    [Theory]
    [InlineData("rus", "rus")]
    [InlineData("RUS", "rus")]
    [InlineData("  Rus  ", "rus")]
    public void Iso6392Code_NormalizesToLowerCase(string value, string expected)
    {
        Assert.Equal(expected, new Iso6392Code(value).Value);
    }

    [Theory]
    [InlineData("ru")]
    [InlineData("russ")]
    public void Iso6392Code_WrongLength_ThrowsArgumentException(string value)
    {
        Assert.Throws<ArgumentException>(() => new Iso6392Code(value));
    }

    [Theory]
    [InlineData("eng", "eng")]
    [InlineData("ENG", "eng")]
    public void Iso6393Code_NormalizesToLowerCase(string value, string expected)
    {
        Assert.Equal(expected, new Iso6393Code(value).Value);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("engl")]
    public void Iso6393Code_WrongLength_ThrowsArgumentException(string value)
    {
        Assert.Throws<ArgumentException>(() => new Iso6393Code(value));
    }

    [Theory]
    [InlineData("12")]
    [InlineData("r-")]
    [InlineData("ру")]
    [InlineData("rу")]
    public void Iso6391Code_NonLatinLetters_ThrowsArgumentException(string value)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Iso6391Code(value));

        Assert.Contains("exactly two Latin letters", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("ru-")]
    [InlineData("рус")]
    [InlineData("ruс")]
    public void Iso6392Code_NonLatinLetters_ThrowsArgumentException(string value)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Iso6392Code(value));

        Assert.Contains("exactly three Latin letters", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("ru-")]
    [InlineData("рус")]
    public void Iso6393Code_NonLatinLetters_ThrowsArgumentException(string value)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Iso6393Code(value));

        Assert.Contains("exactly three Latin letters", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void LanguageCode_EmptyValue_ThrowsArgumentException(string value)
    {
        Assert.Throws<ArgumentException>(() => new Iso6391Code(value));
        Assert.Throws<ArgumentException>(() => new Iso6392Code(value));
        Assert.Throws<ArgumentException>(() => new Iso6393Code(value));
    }

    [Fact]
    public void LanguageCode_NullValue_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Iso6391Code(null!));
        Assert.Throws<ArgumentNullException>(() => new Iso6392Code(null!));
        Assert.Throws<ArgumentNullException>(() => new Iso6393Code(null!));
    }
}
