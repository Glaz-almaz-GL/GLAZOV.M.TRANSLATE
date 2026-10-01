using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Abstractions.TextToSpeech;
using System.Net;
using System.Text;

namespace GLAZOV.M.TRANSLATE.Providers.Microsoft.Tests;

/// <summary>
/// Verifies that a request may name the voice it wants to be spoken with.
/// </summary>
public sealed class MicrosoftNamedVoiceTests
{
    private const string TokenAnswer = """{"t":"the-token","r":"westeurope"}""";

    private static StubHttpMessageHandler Speaking(Action<string> observeSsml)
    {
        return new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri!.Host.Contains("microsofttranslator.com", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(TokenAnswer, Encoding.UTF8, "application/json"),
                };
            }

            observeSsml(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([0x49, 0x44, 0x33, 0x04]),
            };
        });
    }

    [Fact]
    public async Task ExecuteAsync_NamedVoice_SpeaksThatVoice()
    {
        string? ssml = null;

        using StubHttpMessageHandler handler = Speaking(value => ssml = value);
        using HttpClient httpClient = new(handler);
        using MicrosoftTextToSpeechProvider provider = new(httpClient);

        TextToSpeechRequest request = new(
            new ProviderText("Доброе утро"),
            new LanguageId("russian"),
            new VoiceName("ru-RU-DmitryNeural"));

        await provider.ExecuteAsync(request);

        Assert.Contains("ru-RU-DmitryNeural", ssml!, StringComparison.Ordinal);
        Assert.Contains("xml:lang='ru-RU'", ssml!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_NamedVoiceOfAnotherLanguage_IsPassedThrough()
    {
        string? ssml = null;

        using StubHttpMessageHandler handler = Speaking(value => ssml = value);
        using HttpClient httpClient = new(handler);
        using MicrosoftTextToSpeechProvider provider = new(httpClient);

        TextToSpeechRequest request = new(
            new ProviderText("Good morning"),
            new LanguageId("english"),
            new VoiceName("en-GB-RyanNeural"));

        await provider.ExecuteAsync(request);

        Assert.Contains("en-GB-RyanNeural", ssml!, StringComparison.Ordinal);
        Assert.Contains("xml:lang='en-GB'", ssml!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_NamedVoiceForLanguageWithoutDefault_StillSpeaks()
    {
        string? ssml = null;

        using StubHttpMessageHandler handler = Speaking(value => ssml = value);
        using HttpClient httpClient = new(handler);
        using MicrosoftTextToSpeechProvider provider = new(httpClient);

        // Chuvash has no default voice, but a voice named by the caller does
        // not need one.
        TextToSpeechRequest request = new(
            new ProviderText("text"),
            new LanguageId("chuvash"),
            new VoiceName("ru-RU-SvetlanaNeural"));

        await provider.ExecuteAsync(request);

        Assert.Contains("ru-RU-SvetlanaNeural", ssml!, StringComparison.Ordinal);
    }

    [LiveFact]
    public async Task ExecuteAsync_LiveMicrosoftTranslator_SpeaksTheNamedVoice()
    {
        using MicrosoftTextToSpeechProvider provider = new();

        TextToSpeechRequest request = new(
            new ProviderText("Доброе утро"),
            new LanguageId("russian"),
            new VoiceName("ru-RU-DmitryNeural"));

        TextToSpeechResult result = await provider.ExecuteAsync(request);

        Assert.False(result.AudioData.IsEmpty);
    }

    [LiveFact]
    public async Task ExecuteAsync_LiveMicrosoftTranslator_UnknownVoiceFails()
    {
        using MicrosoftTextToSpeechProvider provider = new();

        TextToSpeechRequest request = new(
            new ProviderText("Доброе утро"),
            new LanguageId("russian"),
            new VoiceName("ru-RU-NoSuchNeural"));

        await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(request));
    }
}
