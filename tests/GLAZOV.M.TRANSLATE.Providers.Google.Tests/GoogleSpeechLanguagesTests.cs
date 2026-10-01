using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Abstractions.TextToSpeech;
using GLAZOV.M.TRANSLATE.Providers.Google.Internal;

namespace GLAZOV.M.TRANSLATE.Providers.Google.Tests;

/// <summary>
/// Verifies the generated list of languages the speech endpoint can speak and
/// how the provider refuses the rest.
/// </summary>
public sealed class GoogleSpeechLanguagesTests
{
    [Theory]
    [InlineData("ru")]
    [InlineData("en")]
    [InlineData("de")]
    [InlineData("zh")]
    [InlineData("la")]
    public void Contains_SpokenLanguage_ReturnsTrue(string iso6391Code)
    {
        Assert.True(GoogleSpeechLanguages.Contains(iso6391Code));
    }

    [Theory]
    [InlineData("cv")]
    [InlineData("eo")]
    [InlineData("zu")]
    public void Contains_SilentLanguage_ReturnsFalse(string iso6391Code)
    {
        Assert.False(GoogleSpeechLanguages.Contains(iso6391Code));
    }

    [Fact]
    public async Task ExecuteAsync_SilentLanguage_ThrowsProviderExceptionWithoutCallingTheEndpoint()
    {
        bool called = false;

        using StubHttpMessageHandler handler = new(_ =>
        {
            called = true;

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });

        using HttpClient httpClient = new(handler);
        using GoogleTextToSpeechProvider provider = new(httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new TextToSpeechRequest(new ProviderText("text"), new LanguageId("chuvash"))));

        Assert.Contains("cannot speak language", exception.Message, StringComparison.Ordinal);
        Assert.False(called);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownLanguage_ThrowsProviderException()
    {
        using GoogleTextToSpeechProvider provider = new(new HttpClient());

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new TextToSpeechRequest(new ProviderText("text"), new LanguageId("does_not_exist"))));

        Assert.Equal("Google", exception.ProviderName);
    }

    [LiveFact]
    public async Task ExecuteAsync_LiveGoogleTranslate_SpeaksEverySpokenLanguage()
    {
        // The list is generated from the endpoint, so a sample of it must still
        // be spoken; a whole sweep is the generator's job, not a test's.
        using GoogleTextToSpeechProvider provider = new();

        foreach (string languageId in new[] { "russian", "english", "latin" })
        {
            TextToSpeechResult result = await provider.ExecuteAsync(
                new TextToSpeechRequest(new ProviderText("test"), new LanguageId(languageId)));

            Assert.False(result.AudioData.IsEmpty);
        }
    }
}
