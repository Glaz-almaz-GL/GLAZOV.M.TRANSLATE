using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Providers.Google.Internal;

namespace GLAZOV.M.TRANSLATE.Providers.Google.Tests;

public sealed class GoogleLanguageCodeResolverTests
{
    [Fact]
    public void ToGoogleCode_NullLanguageId_ReturnsAuto()
    {
        string code = GoogleLanguageCodeResolver.ToGoogleCode(null);

        Assert.Equal("auto", code);
    }

    [Theory]
    [InlineData("russian", "ru")]
    [InlineData("english", "en")]
    [InlineData("french", "fr")]
    public void ToGoogleCode_KnownLanguage_ReturnsIso6391Code(string languageId, string expectedCode)
    {
        string code = GoogleLanguageCodeResolver.ToGoogleCode(new LanguageId(languageId));

        Assert.Equal(expectedCode, code);
    }

    [Fact]
    public void ToGoogleCode_UnknownLanguageId_ThrowsTranslationProviderException()
    {
        ProviderException exception = Assert.Throws<ProviderException>(
            () => GoogleLanguageCodeResolver.ToGoogleCode(new LanguageId("does_not_exist")));

        Assert.Equal("Google", exception.ProviderName);
    }

    [Theory]
    [InlineData("ru", "russian")]
    [InlineData("en", "english")]
    [InlineData("EN", "english")]
    public void FromGoogleCode_KnownCode_ReturnsLanguageId(string googleCode, string expectedLanguageId)
    {
        LanguageId languageId = GoogleLanguageCodeResolver.FromGoogleCode(googleCode);

        Assert.Equal(expectedLanguageId, languageId.Value);
    }

    [Fact]
    public void FromGoogleCode_UnknownCode_ThrowsTranslationProviderException()
    {
        ProviderException exception = Assert.Throws<ProviderException>(
            () => GoogleLanguageCodeResolver.FromGoogleCode("zz"));

        Assert.Equal("Google", exception.ProviderName);
    }

    [Fact]
    public void FromGoogleCode_EmptyCode_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => GoogleLanguageCodeResolver.FromGoogleCode(" "));
    }

    [Theory]
    [InlineData("russian")]
    [InlineData("english")]
    [InlineData("french")]
    [InlineData("german")]
    [InlineData("japanese")]
    public void ToGoogleCode_ThenFromGoogleCode_RoundTrips(string languageId)
    {
        LanguageId original = new(languageId);

        string googleCode = GoogleLanguageCodeResolver.ToGoogleCode(original);
        LanguageId roundTripped = GoogleLanguageCodeResolver.FromGoogleCode(googleCode);

        Assert.Equal(original, roundTripped);
    }

    [Theory]
    [InlineData("chinese", "zh-CN")]
    public void ToGoogleCode_LanguageWithItsOwnGoogleCode_ReturnsThatCode(string languageId, string expectedCode)
    {
        string code = GoogleLanguageCodeResolver.ToGoogleCode(new LanguageId(languageId));

        Assert.Equal(expectedCode, code);
    }

    [Theory]
    [InlineData("iw", "hebrew")]
    [InlineData("jw", "javanese")]
    [InlineData("in", "indonesian")]
    [InlineData("ji", "yiddish")]
    [InlineData("mo", "romanian")]
    [InlineData("nb", "norwegian")]
    [InlineData("nn", "norwegian")]
    [InlineData("zh-CN", "chinese")]
    [InlineData("zh-TW", "chinese")]
    [InlineData("zh-Hans", "chinese")]
    [InlineData("zh-Hant", "chinese")]
    [InlineData("ZH-tw", "chinese")]
    public void FromGoogleCode_GoogleOwnCode_ReturnsLanguageId(string googleCode, string expectedLanguageId)
    {
        LanguageId languageId = GoogleLanguageCodeResolver.FromGoogleCode(googleCode);

        Assert.Equal(expectedLanguageId, languageId.Value);
    }

    [Fact]
    public void ToGoogleCode_AndBack_ReturnsTheSameLanguage()
    {
        LanguageId chinese = new("chinese");

        string code = GoogleLanguageCodeResolver.ToGoogleCode(chinese);

        Assert.Equal(chinese, GoogleLanguageCodeResolver.FromGoogleCode(code));
    }
}
