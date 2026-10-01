using GLAZOV.M.TRANSLATE.Providers.Microsoft.Internal;

namespace GLAZOV.M.TRANSLATE.Providers.Microsoft.Tests;

/// <summary>
/// Verifies the generated table of default voices and the locale a voice
/// name carries.
/// </summary>
public sealed class MicrosoftVoicesTests
{
    [Theory]
    [InlineData("ru")]
    [InlineData("en")]
    [InlineData("de")]
    [InlineData("zh")]
    public void TryGetDefault_SpokenLanguage_ReturnsVoiceOfThatLanguage(string iso6391Code)
    {
        Assert.True(MicrosoftVoices.TryGetDefault(iso6391Code, out string voiceName));
        Assert.StartsWith($"{iso6391Code}-", voiceName, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ru", "ru-RU")]
    [InlineData("en", "en-US")]
    [InlineData("es", "es-ES")]
    [InlineData("zh", "zh-CN")]
    public void TryGetDefault_LanguageOfManyRegions_PrefersTheExpectedLocale(string iso6391Code, string expectedLocale)
    {
        Assert.True(MicrosoftVoices.TryGetDefault(iso6391Code, out string voiceName));
        Assert.Equal(expectedLocale, MicrosoftVoices.GetLocale(voiceName));
    }

    [Fact]
    public void TryGetDefault_UnspokenLanguage_ReturnsFalse()
    {
        Assert.False(MicrosoftVoices.TryGetDefault("cv", out _));
    }

    [Theory]
    [InlineData("ru-RU-SvetlanaNeural", "ru-RU")]
    [InlineData("sr-Latn-RS-NicholasNeural", "sr-Latn-RS")]
    [InlineData("zh-CN-Xiaoxiao:DragonHDFlashLatestNeural", "zh-CN")]
    [InlineData("Svetlana", "Svetlana")]
    public void GetLocale_ReturnsEverythingButTheNameOfTheVoice(string voiceName, string expectedLocale)
    {
        Assert.Equal(expectedLocale, MicrosoftVoices.GetLocale(voiceName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GetLocale_EmptyName_ThrowsArgumentException(string voiceName)
    {
        Assert.Throws<ArgumentException>(() => MicrosoftVoices.GetLocale(voiceName));
    }
}
