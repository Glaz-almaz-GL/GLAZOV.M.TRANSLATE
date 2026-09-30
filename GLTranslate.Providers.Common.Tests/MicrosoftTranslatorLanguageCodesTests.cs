using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;

namespace GLTranslate.Providers.Common.Tests;

/// <summary>
/// Verifies the table of how the Microsoft translation service writes
/// languages, which the providers built on it share.
/// </summary>
public sealed class MicrosoftTranslatorLanguageCodesTests
{
    [Theory]
    [InlineData("chinese", "zh-Hans")]
    [InlineData("serbian", "sr-Cyrl")]
    [InlineData("norwegian", "nb")]
    [InlineData("english", "en")]
    public void CreateResolver_WritesLanguagesTheWayMicrosoftDoes(string languageId, string expected)
    {
        LanguageCodeResolver resolver = MicrosoftTranslatorLanguageCodes.CreateResolver("Any");

        Assert.Equal(expected, resolver.ToProviderCode(new LanguageId(languageId)));
    }

    [Theory]
    [InlineData("zh-Hans", "chinese")]
    [InlineData("zh-Hant", "chinese")]
    [InlineData("nb", "norwegian")]
    [InlineData("nn", "norwegian")]
    [InlineData("sr-Latn", "serbian")]
    public void CreateResolver_ReadsLanguagesTheWayMicrosoftAnswers(string code, string expected)
    {
        LanguageCodeResolver resolver = MicrosoftTranslatorLanguageCodes.CreateResolver("Any");

        Assert.Equal(expected, resolver.FromProviderCode(code).Value);
    }

    [Fact]
    public void CreateResolver_EachProviderGetsAResolverUnderItsOwnName()
    {
        LanguageCodeResolver bing = MicrosoftTranslatorLanguageCodes.CreateResolver("Bing");
        LanguageCodeResolver microsoft = MicrosoftTranslatorLanguageCodes.CreateResolver("Microsoft");

        Assert.Equal("Bing", Assert.Throws<ProviderException>(() => bing.FromProviderCode("zz")).ProviderName);
        Assert.Equal("Microsoft", Assert.Throws<ProviderException>(() => microsoft.FromProviderCode("zz")).ProviderName);
    }

    [Fact]
    public void CreateResolver_NullName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => MicrosoftTranslatorLanguageCodes.CreateResolver(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void CreateResolver_BlankName_ThrowsArgumentException(string name)
    {
        Assert.Throws<ArgumentException>(() => MicrosoftTranslatorLanguageCodes.CreateResolver(name));
    }
}
