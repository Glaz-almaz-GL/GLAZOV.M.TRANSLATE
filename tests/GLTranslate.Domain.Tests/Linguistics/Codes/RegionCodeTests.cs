using GLTranslate.Domain.Linguistics.Regions.Codes;

namespace GLTranslate.Domain.Tests.Linguistics.Codes;

/// <summary>
/// Verifies normalization and validation of the ISO 3166-1 region codes.
/// </summary>
public sealed class RegionCodeTests
{
    [Theory]
    [InlineData("ru", "RU")]
    [InlineData("RU", "RU")]
    [InlineData("  ru  ", "RU")]
    public void Iso3166Alpha2Code_NormalizesToUpperCase(string value, string expected)
    {
        Assert.Equal(expected, new Iso3166Alpha2Code(value).Value);
    }

    [Theory]
    [InlineData("R")]
    [InlineData("RUS")]
    [InlineData("R1")]
    public void Iso3166Alpha2Code_InvalidValue_ThrowsArgumentException(string value)
    {
        Assert.Throws<ArgumentException>(() => new Iso3166Alpha2Code(value));
    }

    [Theory]
    [InlineData("rus", "RUS")]
    [InlineData("RUS", "RUS")]
    public void Iso3166Alpha3Code_NormalizesToUpperCase(string value, string expected)
    {
        Assert.Equal(expected, new Iso3166Alpha3Code(value).Value);
    }

    [Theory]
    [InlineData("RU")]
    [InlineData("RUSS")]
    [InlineData("RU1")]
    public void Iso3166Alpha3Code_InvalidValue_ThrowsArgumentException(string value)
    {
        Assert.Throws<ArgumentException>(() => new Iso3166Alpha3Code(value));
    }

    [Theory]
    [InlineData("643", "643")]
    [InlineData("  840  ", "840")]
    [InlineData("008", "008")]
    public void Iso3166NumericCode_KeepsLeadingZeros(string value, string expected)
    {
        Assert.Equal(expected, new Iso3166NumericCode(value).Value);
    }

    [Theory]
    [InlineData("64")]
    [InlineData("6430")]
    [InlineData("64A")]
    public void Iso3166NumericCode_InvalidValue_ThrowsArgumentException(string value)
    {
        Assert.Throws<ArgumentException>(() => new Iso3166NumericCode(value));
    }

    [Theory]
    [InlineData("РУ")]
    [InlineData("ΕΛ")]
    [InlineData("RУ")]
    public void Iso3166Alpha2Code_NonLatinLetters_ThrowsArgumentException(string value)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Iso3166Alpha2Code(value));

        Assert.Contains("exactly two letters", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("РУС")]
    [InlineData("ΕΛΛ")]
    [InlineData("RUС")]
    public void Iso3166Alpha3Code_NonLatinLetters_ThrowsArgumentException(string value)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Iso3166Alpha3Code(value));

        Assert.Contains("exactly three letters", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RegionCode_NullValue_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Iso3166Alpha2Code(null!));
        Assert.Throws<ArgumentNullException>(() => new Iso3166Alpha3Code(null!));
        Assert.Throws<ArgumentNullException>(() => new Iso3166NumericCode(null!));
    }
}
