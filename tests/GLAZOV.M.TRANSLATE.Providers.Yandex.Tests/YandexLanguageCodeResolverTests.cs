using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Providers.Yandex.Internal;

namespace GLAZOV.M.TRANSLATE.Providers.Yandex.Tests;

/// <summary>
/// Verifies the translation between domain language identifiers and the codes
/// the Yandex endpoints speak.
/// </summary>
public sealed class YandexLanguageCodeResolverTests
{
    [Theory]
    [InlineData("russian", "ru")]
    [InlineData("english", "en")]
    [InlineData("chinese", "zh")]
    public void ToYandexCode_KnownLanguage_ReturnsIso6391Code(string languageId, string expectedCode)
    {
        Assert.Equal(expectedCode, YandexLanguageCodeResolver.ToYandexCode(new LanguageId(languageId)));
    }

    [Fact]
    public void ToYandexCode_UnknownLanguage_ThrowsProviderException()
    {
        ProviderException exception = Assert.Throws<ProviderException>(
            () => YandexLanguageCodeResolver.ToYandexCode(new LanguageId("does_not_exist")));

        Assert.Equal("Yandex", exception.ProviderName);
    }

    [Fact]
    public void ToYandexCode_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => YandexLanguageCodeResolver.ToYandexCode(null!));
    }

    [Theory]
    [InlineData("ru", "russian")]
    [InlineData("EN", "english")]
    [InlineData("  fr  ", "french")]
    public void FromYandexCode_KnownCode_ReturnsLanguageId(string code, string expectedLanguageId)
    {
        Assert.Equal(expectedLanguageId, YandexLanguageCodeResolver.FromYandexCode(code).Value);
    }

    [Fact]
    public void FromYandexCode_UnknownCode_ThrowsProviderException()
    {
        ProviderException exception = Assert.Throws<ProviderException>(
            () => YandexLanguageCodeResolver.FromYandexCode("zz"));

        Assert.Equal("Yandex", exception.ProviderName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FromYandexCode_EmptyCode_ThrowsArgumentException(string code)
    {
        Assert.Throws<ArgumentException>(() => YandexLanguageCodeResolver.FromYandexCode(code));
    }

    [Theory]
    [InlineData("russian")]
    [InlineData("english")]
    [InlineData("chinese")]
    public void ToYandexCode_AndBack_ReturnsTheSameLanguage(string languageId)
    {
        LanguageId language = new(languageId);

        Assert.Equal(language, YandexLanguageCodeResolver.FromYandexCode(YandexLanguageCodeResolver.ToYandexCode(language)));
    }
}
