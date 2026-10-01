using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Domain.Linguistics.Languages;
using GLTranslate.Domain.Linguistics.Languages.Codes;
using GLTranslate.Domain.Linguistics.Scripts;

namespace GLTranslate.Domain.Tests.Linguistics.Languages;

/// <summary>
/// Verifies construction-time validation and equality of the
/// <see cref="Language"/> entity.
/// </summary>
public sealed class LanguageTests
{
    private static Script Latin => ScriptRegistry.Default.Latin;

    private static Script Cyrillic => ScriptRegistry.Default.Cyrillic;

    private static Language Create(
        string id = "russian",
        string name = "Russian",
        string nativeName = "Russian",
        IEnumerable<Script>? scripts = null)
    {
        return new Language(
            new LanguageId(id),
            name,
            nativeName,
            scripts ?? [Cyrillic],
            [new Iso6391Code("ru")]);
    }

    [Fact]
    public void Constructor_ValidArguments_ExposesEveryValue()
    {
        Language language = Create();

        Assert.Equal("russian", language.Id.Value);
        Assert.Equal("Russian", language.Name);
        Assert.Equal("Russian", language.NativeName);
        Assert.Equal("ru", language.Codes.Get<Iso6391Code>().Value);
        Assert.Equal(Cyrillic, language.Scripts[0]);
    }

    [Fact]
    public void Constructor_SeveralScripts_PreservesOrder()
    {
        Language language = Create(scripts: [Cyrillic, Latin]);

        Assert.Equal(2, language.Scripts.Count);
        Assert.Equal(Cyrillic, language.Scripts[0]);
        Assert.Equal(Latin, language.Scripts[1]);
    }

    [Fact]
    public void Constructor_DuplicateScript_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Create(scripts: [Latin, Latin]));
    }

    [Fact]
    public void Constructor_NullId_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new Language(null!, "Russian", "Russian", [Cyrillic], [new Iso6391Code("ru")]));
    }

    [Fact]
    public void Constructor_NullScripts_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new Language(new LanguageId("russian"), "Russian", "Russian", null!, [new Iso6391Code("ru")]));
    }

    [Fact]
    public void Constructor_NullCodes_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new Language(new LanguageId("russian"), "Russian", "Russian", [Cyrillic], null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyName_ThrowsArgumentException(string name)
    {
        Assert.Throws<ArgumentException>(() => Create(name: name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyNativeName_ThrowsArgumentException(string nativeName)
    {
        Assert.Throws<ArgumentException>(() => Create(nativeName: nativeName));
    }

    [Fact]
    public void Equals_SameId_ReturnsTrue()
    {
        Language left = Create(name: "Russian");
        Language right = Create(name: "Other name", scripts: [Latin]);

        Assert.True(left.Equals(right));
        Assert.True(left == right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentId_ReturnsFalse()
    {
        Assert.False(Create(id: "russian").Equals(Create(id: "english")));
        Assert.True(Create(id: "russian") != Create(id: "english"));
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        Language language = Create();

        Assert.False(language.Equals(null));
        Assert.False(language.Equals((object?)null));
    }

    [Fact]
    public void ToString_ReturnsName()
    {
        Assert.Equal("Russian", Create().ToString());
    }
}
