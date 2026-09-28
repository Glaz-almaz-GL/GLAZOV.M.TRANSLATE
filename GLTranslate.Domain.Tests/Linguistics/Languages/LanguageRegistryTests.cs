using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Linguistics.Scripts;
using GLTranslate.Domain.Linguistics.Languages;
using GLTranslate.Domain.Linguistics.Languages.Codes;
using GLTranslate.Domain.Linguistics.Scripts;
using GLTranslate.Domain.Linguistics.Scripts.Codes;

namespace GLTranslate.Domain.Tests.Linguistics.Languages;

/// <summary>
/// Verifies the lookup contract of <see cref="LanguageRegistry"/> and the
/// consistency of the generated language data.
/// </summary>
public sealed class LanguageRegistryTests
{
    private static LanguageRegistry Registry => LanguageRegistry.Default;

    [Fact]
    public void Get_KnownId_ReturnsLanguage()
    {
        Language language = Registry.Get(new LanguageId("russian"));

        Assert.Equal("Russian", language.Name);
    }

    [Fact]
    public void Get_UnknownId_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() => Registry.Get(new LanguageId("does_not_exist")));
    }

    [Fact]
    public void Get_NullId_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Registry.Get(null!));
    }

    [Fact]
    public void TryGet_UnknownId_ReturnsFalse()
    {
        Assert.False(Registry.TryGet(new LanguageId("does_not_exist"), out Language? language));
        Assert.Null(language);
    }

    [Fact]
    public void Contains_ReflectsRegistryContent()
    {
        Assert.True(Registry.Contains(new LanguageId("english")));
        Assert.False(Registry.Contains(new LanguageId("does_not_exist")));
    }

    [Fact]
    public void All_ContainsEveryLanguageExactlyOnce()
    {
        Assert.NotEmpty(Registry.All);
        Assert.Equal(Registry.All.Count, Registry.All.Select(x => x.Id).Distinct().Count());
    }

    [Fact]
    public void All_EveryLanguageHasIso6391CodeAndAtLeastOneScript()
    {
        foreach (Language language in Registry.All)
        {
            Assert.True(language.Codes.Contains<Iso6391Code>());
            Assert.Equal(2, language.Codes.Get<Iso6391Code>().Value.Length);
            Assert.NotEmpty(language.Scripts);
        }
    }

    [Fact]
    public void All_ScriptsComeFromTheScriptRegistry()
    {
        foreach (Language language in Registry.All)
        {
            foreach (Script script in language.Scripts)
            {
                Assert.True(ScriptRegistry.Default.Contains(script.Id));
            }
        }
    }

    [Theory]
    [InlineData("arabic", "Arab")]
    [InlineData("hebrew", "Hebr")]
    [InlineData("persian", "Arab")]
    [InlineData("divehi", "Thaa")]
    public void RightToLeftLanguages_AreWrittenInRightToLeftScript(string languageId, string iso15924)
    {
        Language language = Registry.Get(new LanguageId(languageId));
        Script script = language.Scripts[0];

        Assert.Equal(iso15924, script.Codes.Get<Iso15924Code>().Value);
        Assert.Equal(WritingDirection.RightToLeft, script.Direction);
    }

    [Theory]
    [InlineData("russian", "Cyrl")]
    [InlineData("english", "Latn")]
    [InlineData("greek", "Grek")]
    public void LeftToRightLanguages_AreWrittenInLeftToRightScript(string languageId, string iso15924)
    {
        Language language = Registry.Get(new LanguageId(languageId));
        Script script = language.Scripts[0];

        Assert.Equal(iso15924, script.Codes.Get<Iso15924Code>().Value);
        Assert.Equal(WritingDirection.LeftToRight, script.Direction);
    }

    [Fact]
    public void Kashmiri_IsWrittenInBothArabicAndDevanagari()
    {
        Language language = Registry.Get(new LanguageId("kashmiri"));

        Assert.Equal(2, language.Scripts.Count);
        Assert.Equal(WritingDirection.RightToLeft, language.Scripts[0].Direction);
        Assert.Equal(WritingDirection.LeftToRight, language.Scripts[1].Direction);
    }
}
