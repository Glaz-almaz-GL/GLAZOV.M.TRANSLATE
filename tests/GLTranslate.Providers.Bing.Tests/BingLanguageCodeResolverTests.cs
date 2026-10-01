using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Providers.Bing.Internal;

namespace GLTranslate.Providers.Bing.Tests;

/// <summary>
/// Verifies the translation between domain language identifiers and the codes
/// the Bing endpoints speak.
/// </summary>
public sealed class BingLanguageCodeResolverTests
{
    [Theory]
    [InlineData("russian", "ru")]
    [InlineData("english", "en")]
    [InlineData("french", "fr")]
    public void ToBingCode_KnownLanguage_ReturnsIso6391Code(string languageId, string expectedCode)
    {
        Assert.Equal(expectedCode, BingLanguageCodeResolver.ToBingCode(new LanguageId(languageId)));
    }

    [Fact]
    public void ToBingCode_UnknownLanguage_ThrowsProviderException()
    {
        ProviderException exception = Assert.Throws<ProviderException>(
            () => BingLanguageCodeResolver.ToBingCode(new LanguageId("does_not_exist")));

        Assert.Equal("Bing", exception.ProviderName);
    }

    [Fact]
    public void ToBingCode_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => BingLanguageCodeResolver.ToBingCode(null!));
    }

    [Theory]
    [InlineData("ru", "russian")]
    [InlineData("EN", "english")]
    [InlineData("  fr  ", "french")]
    public void FromBingCode_KnownCode_ReturnsLanguageId(string code, string expectedLanguageId)
    {
        Assert.Equal(expectedLanguageId, BingLanguageCodeResolver.FromBingCode(code).Value);
    }

    [Fact]
    public void FromBingCode_UnknownCode_ThrowsProviderException()
    {
        ProviderException exception = Assert.Throws<ProviderException>(
            () => BingLanguageCodeResolver.FromBingCode("zz"));

        Assert.Equal("Bing", exception.ProviderName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FromBingCode_EmptyCode_ThrowsArgumentException(string code)
    {
        Assert.Throws<ArgumentException>(() => BingLanguageCodeResolver.FromBingCode(code));
    }

    [Theory]
    [InlineData("chinese", "zh-Hans")]
    [InlineData("serbian", "sr-Cyrl")]
    [InlineData("mongolian", "mn-Cyrl")]
    [InlineData("norwegian", "nb")]
    public void ToBingCode_LanguageWithItsOwnCode_ReturnsThatCode(string languageId, string expectedCode)
    {
        Assert.Equal(expectedCode, BingLanguageCodeResolver.ToBingCode(new LanguageId(languageId)));
    }

    [Theory]
    [InlineData("zh-Hans", "chinese")]
    [InlineData("sr-Latn", "serbian")]
    [InlineData("nb", "norwegian")]
    public void FromBingCode_BingOwnCode_ReturnsLanguageId(string code, string expectedLanguageId)
    {
        Assert.Equal(expectedLanguageId, BingLanguageCodeResolver.FromBingCode(code).Value);
    }

    [Theory]
    [InlineData("russian")]
    [InlineData("english")]
    [InlineData("chinese")]
    public void ToBingCode_AndBack_ReturnsTheSameLanguage(string languageId)
    {
        LanguageId language = new(languageId);

        Assert.Equal(language, BingLanguageCodeResolver.FromBingCode(BingLanguageCodeResolver.ToBingCode(language)));
    }
}
