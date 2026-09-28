using GLTranslate.Domain.Linguistics.Scripts.Code;

namespace GLTranslate.Domain.Tests.Linguistics.Codes;

/// <summary>
/// Verifies normalization and validation of the ISO 15924 script codes.
/// </summary>
public sealed class ScriptCodeTests
{
    [Theory]
    [InlineData("Latn", "Latn")]
    [InlineData("latn", "Latn")]
    [InlineData("LATN", "Latn")]
    [InlineData("  cyrl  ", "Cyrl")]
    public void Iso15924Code_NormalizesToTitleCase(string value, string expected)
    {
        Assert.Equal(expected, new Iso15924Code(value).Value);
    }

    [Theory]
    [InlineData("Lat")]
    [InlineData("Latin")]
    public void Iso15924Code_WrongLength_ThrowsArgumentException(string value)
    {
        Assert.Throws<ArgumentException>(() => new Iso15924Code(value));
    }

    [Theory]
    [InlineData("Lat1")]
    [InlineData("12ab")]
    public void Iso15924Code_NonLetterValue_ThrowsArgumentException(string value)
    {
        Assert.Throws<ArgumentException>(() => new Iso15924Code(value));
    }

    [Theory]
    [InlineData("Кирл")]
    [InlineData("Λατν")]
    [InlineData("Latа")]
    public void Iso15924Code_NonLatinLetters_ThrowsArgumentException(string value)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Iso15924Code(value));

        Assert.Contains("only Latin letters", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Iso15924Code_NullValue_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Iso15924Code(null!));
    }
}
