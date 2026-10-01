using GLTranslate.Domain.Linguistics.Cultures.Codes;

namespace GLTranslate.Domain.Tests.Linguistics.Codes;

/// <summary>
/// Verifies normalization and validation of the BCP 47 culture codes.
/// </summary>
public sealed class CultureCodeTests
{
    [Theory]
    [InlineData("en", "en")]
    [InlineData("EN", "en")]
    [InlineData("  ru  ", "ru")]
    [InlineData("ru-ru", "ru-RU")]
    [InlineData("RU-RU", "ru-RU")]
    [InlineData("zh-hans", "zh-Hans")]
    [InlineData("zh-HANT-tw", "zh-Hant-TW")]
    [InlineData("es-419", "es-419")]
    [InlineData("en-US-POSIX", "en-US-POSIX")]
    [InlineData("gsw", "gsw")]
    public void Bcp47Code_NormalizesSubtagCasing(string value, string expected)
    {
        Assert.Equal(expected, new Bcp47Code(value).Value);
    }

    [Theory]
    [InlineData("e")]
    [InlineData("englishlang")]
    [InlineData("12")]
    [InlineData("-US")]
    [InlineData("ру")]
    public void Bcp47Code_MalformedLanguage_ThrowsArgumentException(string value)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Bcp47Code(value));

        Assert.Contains("must start with a language subtag", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("en-U")]
    [InlineData("en-US-a")]
    [InlineData("en-x-private")]
    [InlineData("en-US-1")]
    public void Bcp47Code_UnsupportedSubtag_ThrowsArgumentException(string value)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Bcp47Code(value));

        Assert.Contains("is not a script, region or variant subtag", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Bcp47Code_EmptyValue_ThrowsArgumentException(string value)
    {
        Assert.Throws<ArgumentException>(() => new Bcp47Code(value));
    }

    [Fact]
    public void Bcp47Code_NullValue_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Bcp47Code(null!));
    }
}
