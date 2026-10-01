using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Providers.Baidu.Internal;

namespace GLAZOV.M.TRANSLATE.Providers.Baidu.Tests;

/// <summary>
/// Verifies the codes Baidu knows languages by.
/// </summary>
public sealed class BaiduLanguageCodeResolverTests
{
    [Theory]
    [InlineData("english", "en")]
    [InlineData("russian", "ru")]
    [InlineData("chinese", "zh")]
    [InlineData("german", "de")]
    [InlineData("japanese", "jp")]
    [InlineData("korean", "kor")]
    [InlineData("french", "fra")]
    public void ToBaiduCode_KnownLanguage_ReturnsTheCodeBaiduUses(string languageId, string expected)
    {
        Assert.Equal(expected, BaiduLanguageCodeResolver.ToBaiduCode(new LanguageId(languageId)));
    }

    [Theory]
    [InlineData("en", "english")]
    [InlineData("jp", "japanese")]
    [InlineData("kor", "korean")]
    [InlineData("fra", "french")]
    [InlineData("FRA", "french")]
    public void FromBaiduCode_KnownCode_ReturnsTheLanguage(string code, string expected)
    {
        Assert.Equal(expected, BaiduLanguageCodeResolver.FromBaiduCode(code).Value);
    }

    [Fact]
    public void FromBaiduCode_CodeWithNoLanguage_ThrowsProviderException()
    {
        // "cht" is Baidu's own name for traditional Chinese, which GLAZOV.M.TRANSLATE
        // does not tell apart from Chinese.
        ProviderException exception = Assert.Throws<ProviderException>(() => BaiduLanguageCodeResolver.FromBaiduCode("cht"));

        Assert.Equal("Baidu", exception.ProviderName);
    }

    [Fact]
    public void ToBaiduCode_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => BaiduLanguageCodeResolver.ToBaiduCode(null!));
    }
}
