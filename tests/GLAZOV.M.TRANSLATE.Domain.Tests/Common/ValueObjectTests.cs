using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Scripts;
using GLAZOV.M.TRANSLATE.Domain.Linguistics.Languages.Codes;

namespace GLAZOV.M.TRANSLATE.Domain.Tests.Common;

/// <summary>
/// Verifies the equality contract of value objects: equality by runtime type
/// and value, hash-code consistency and string normalization.
/// </summary>
public sealed class ValueObjectTests
{
    [Fact]
    public void Equals_SameTypeSameValue_ReturnsTrue()
    {
        Iso6391Code left = new("ru");
        Iso6391Code right = new("ru");

        Assert.True(left.Equals(right));
        Assert.True(left == right);
        Assert.False(left != right);
    }

    [Fact]
    public void Equals_SameTypeDifferentValue_ReturnsFalse()
    {
        Iso6391Code left = new("ru");
        Iso6391Code right = new("en");

        Assert.False(left.Equals(right));
        Assert.True(left != right);
    }

    [Fact]
    public void Equals_DifferentTypeSameValue_ReturnsFalse()
    {
        LanguageCode iso2 = new Iso6392Code("rus");
        LanguageCode iso3 = new Iso6393Code("rus");

        Assert.Equal(iso2.Value, iso3.Value);
        Assert.False(iso2.Equals(iso3));
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        Iso6391Code code = new("ru");

        Assert.False(code.Equals(null));
        Assert.False(code.Equals((object?)null));
    }

    [Fact]
    public void GetHashCode_EqualValues_AreEqual()
    {
        Iso6391Code left = new("ru");
        Iso6391Code right = new("ru");

        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentTypesSameValue_AreNotEqual()
    {
        Iso6392Code iso2 = new("rus");
        Iso6393Code iso3 = new("rus");

        Assert.NotEqual(iso2.GetHashCode(), iso3.GetHashCode());
    }

    [Fact]
    public void Operators_BothNull_AreEqual()
    {
        ScriptId? left = null;
        ScriptId? right = null;

        Assert.True(left == right);
        Assert.False(left != right);
    }

    [Fact]
    public void Operators_OneNull_AreNotEqual()
    {
        ScriptId left = new("latin");
        ScriptId? right = null;

        Assert.False(left == right);
        Assert.True(left != right);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        ScriptId id = new("latin");

        Assert.Equal("latin", id.ToString());
    }

    [Theory]
    [InlineData("  latin  ", "latin")]
    [InlineData("latin", "latin")]
    public void StringValueObject_TrimsSurroundingWhiteSpace(string value, string expected)
    {
        ScriptId id = new(value);

        Assert.Equal(expected, id.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void StringValueObject_EmptyValue_ThrowsArgumentException(string value)
    {
        Assert.Throws<ArgumentException>(() => new ScriptId(value));
    }

    [Fact]
    public void StringValueObject_NullValue_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ScriptId(null!));
    }
}
