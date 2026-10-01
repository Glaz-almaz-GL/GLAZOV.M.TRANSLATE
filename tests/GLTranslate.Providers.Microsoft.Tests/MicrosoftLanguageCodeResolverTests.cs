using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Providers.Microsoft.Internal;

namespace GLTranslate.Providers.Microsoft.Tests;

/// <summary>
/// Verifies the translation between domain language identifiers and the
/// codes Microsoft Translator speaks.
/// </summary>
public sealed class MicrosoftLanguageCodeResolverTests
{
    [Theory]
    [InlineData("russian", "ru")]
    [InlineData("english", "en")]
    [InlineData("french", "fr")]
    public void ToMicrosoftCode_KnownLanguage_ReturnsIso6391Code(string languageId, string expectedCode)
    {
        Assert.Equal(expectedCode, MicrosoftLanguageCodeResolver.ToMicrosoftCode(new LanguageId(languageId)));
    }

    [Theory]
    [InlineData("chinese", "zh-Hans")]
    [InlineData("serbian", "sr-Cyrl")]
    [InlineData("mongolian", "mn-Cyrl")]
    [InlineData("norwegian", "nb")]
    public void ToMicrosoftCode_LanguageWithItsOwnCode_ReturnsThatCode(string languageId, string expectedCode)
    {
        Assert.Equal(expectedCode, MicrosoftLanguageCodeResolver.ToMicrosoftCode(new LanguageId(languageId)));
    }

    [Fact]
    public void ToMicrosoftCode_UnknownLanguage_ThrowsProviderException()
    {
        ProviderException exception = Assert.Throws<ProviderException>(
            () => MicrosoftLanguageCodeResolver.ToMicrosoftCode(new LanguageId("does_not_exist")));

        Assert.Equal("Microsoft", exception.ProviderName);
    }

    [Fact]
    public void ToMicrosoftCode_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => MicrosoftLanguageCodeResolver.ToMicrosoftCode(null!));
    }

    [Theory]
    [InlineData("ru", "russian")]
    [InlineData("EN", "english")]
    [InlineData("  fr  ", "french")]
    public void FromMicrosoftCode_KnownCode_ReturnsLanguageId(string code, string expectedLanguageId)
    {
        Assert.Equal(expectedLanguageId, MicrosoftLanguageCodeResolver.FromMicrosoftCode(code).Value);
    }

    [Theory]
    [InlineData("zh-Hans", "chinese")]
    [InlineData("zh-Hant", "chinese")]
    [InlineData("sr-Cyrl", "serbian")]
    [InlineData("sr-Latn", "serbian")]
    [InlineData("mn-Cyrl", "mongolian")]
    [InlineData("nb", "norwegian")]
    [InlineData("nn", "norwegian")]
    [InlineData("lug", "ganda")]
    public void FromMicrosoftCode_MicrosoftOwnCode_ReturnsLanguageId(string code, string expectedLanguageId)
    {
        Assert.Equal(expectedLanguageId, MicrosoftLanguageCodeResolver.FromMicrosoftCode(code).Value);
    }

    [Fact]
    public void FromMicrosoftCode_UnknownCode_ThrowsProviderException()
    {
        ProviderException exception = Assert.Throws<ProviderException>(
            () => MicrosoftLanguageCodeResolver.FromMicrosoftCode("zz"));

        Assert.Equal("Microsoft", exception.ProviderName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FromMicrosoftCode_EmptyCode_ThrowsArgumentException(string code)
    {
        Assert.Throws<ArgumentException>(() => MicrosoftLanguageCodeResolver.FromMicrosoftCode(code));
    }

    [Theory]
    [InlineData("chinese")]
    [InlineData("serbian")]
    [InlineData("norwegian")]
    [InlineData("russian")]
    public void ToMicrosoftCode_AndBack_ReturnsTheSameLanguage(string languageId)
    {
        LanguageId language = new(languageId);

        string code = MicrosoftLanguageCodeResolver.ToMicrosoftCode(language);

        Assert.Equal(language, MicrosoftLanguageCodeResolver.FromMicrosoftCode(code));
    }
}
