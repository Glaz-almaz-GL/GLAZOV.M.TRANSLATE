using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.TextToSpeech;
using System.Net;
using System.Text;

namespace GLTranslate.Providers.Microsoft.Tests;

/// <summary>
/// Verifies the text-to-speech provider: the token it fetches, the request it
/// builds and the result it returns.
/// </summary>
public sealed class MicrosoftTextToSpeechProviderTests
{
    private const string TokenAnswer = """{"t":"the-token","r":"westeurope"}""";

    private static readonly byte[] Audio = [0x49, 0x44, 0x33, 0x04];

    /// <summary>
    /// Answers the token request with a token and every other request with audio,
    /// handing each request to the observer first.
    /// </summary>
    private static StubHttpMessageHandler Speaking(Action<HttpRequestMessage>? observe = null)
    {
        return new StubHttpMessageHandler(request =>
        {
            observe?.Invoke(request);

            if (request.RequestUri!.Host.Contains("microsofttranslator.com", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(TokenAnswer, Encoding.UTF8, "application/json"),
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(Audio),
            };
        });
    }

    [Fact]
    public void Name_IsMicrosoft()
    {
        using MicrosoftTextToSpeechProvider provider = new(new HttpClient());

        Assert.Equal("Microsoft", provider.Name);
    }

    [Fact]
    public async Task ExecuteAsync_Answer_ReturnsAudio()
    {
        using StubHttpMessageHandler handler = Speaking();
        using HttpClient httpClient = new(handler);
        using MicrosoftTextToSpeechProvider provider = new(httpClient);

        TextToSpeechRequest request = new(new ProviderText("Доброе утро"), new LanguageId("russian"));
        TextToSpeechResult result = await provider.ExecuteAsync(request);

        Assert.Equal(Audio, result.AudioData.ToArray());
        Assert.Equal("audio/mpeg", result.ContentType.Value);
        Assert.Equal("russian", result.LanguageId.Value);
        Assert.Equal(request.Id, result.RequestId);
    }

    [Fact]
    public async Task ExecuteAsync_SpeaksTheVoiceOfTheLanguageInTheRegionOfTheToken()
    {
        string? host = null;
        string? token = null;
        string? ssml = null;

        using StubHttpMessageHandler handler = Speaking(request =>
        {
            if (request.RequestUri!.Host.Contains("tts.speech", StringComparison.Ordinal))
            {
                host = request.RequestUri.Host;
                token = request.Headers.Authorization!.Parameter;
                ssml = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            }
        });

        using HttpClient httpClient = new(handler);
        using MicrosoftTextToSpeechProvider provider = new(httpClient);

        await provider.ExecuteAsync(new TextToSpeechRequest(new ProviderText("Доброе утро"), new LanguageId("russian")));

        Assert.Equal("westeurope.tts.speech.microsoft.com", host);
        Assert.Equal("the-token", token);
        Assert.Contains("ru-RU-DariyaNeural", ssml!, StringComparison.Ordinal);
        Assert.Contains("Доброе утро", ssml!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_SecondCall_ReusesTheToken()
    {
        List<HttpRequestMessage> requests = [];

        using StubHttpMessageHandler handler = Speaking(requests.Add);
        using HttpClient httpClient = new(handler);
        using MicrosoftTextToSpeechProvider provider = new(httpClient);

        await provider.ExecuteAsync(new TextToSpeechRequest(new ProviderText("Доброе утро"), new LanguageId("russian")));
        await provider.ExecuteAsync(new TextToSpeechRequest(new ProviderText("Добрый вечер"), new LanguageId("russian")));

        Assert.Equal(3, requests.Count);
    }

    [Fact]
    public async Task ExecuteAsync_LanguageWithoutVoice_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Speaking();
        using HttpClient httpClient = new(handler);
        using MicrosoftTextToSpeechProvider provider = new(httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new TextToSpeechRequest(new ProviderText("text"), new LanguageId("chuvash"))));

        Assert.Contains("has no voice", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownLanguage_ThrowsProviderException()
    {
        using MicrosoftTextToSpeechProvider provider = new(new HttpClient());

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new TextToSpeechRequest(new ProviderText("text"), new LanguageId("does_not_exist"))));

        Assert.Equal("Microsoft", exception.ProviderName);
    }

    [Fact]
    public async Task ExecuteAsync_TokenRequestFails_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        using HttpClient httpClient = new(handler);
        using MicrosoftTextToSpeechProvider provider = new(httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new TextToSpeechRequest(new ProviderText("text"), new LanguageId("russian"))));

        Assert.Contains("speech token", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_TokenAnswerWithoutToken_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json"),
        });

        using HttpClient httpClient = new(handler);
        using MicrosoftTextToSpeechProvider provider = new(httpClient);

        await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new TextToSpeechRequest(new ProviderText("text"), new LanguageId("russian"))));
    }

    [Fact]
    public async Task ExecuteAsync_EmptyAudio_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = new(request =>
            request.RequestUri!.Host.Contains("microsofttranslator.com", StringComparison.Ordinal)
                ? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(TokenAnswer, Encoding.UTF8, "application/json"),
                }
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([]) });

        using HttpClient httpClient = new(handler);
        using MicrosoftTextToSpeechProvider provider = new(httpClient);

        await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new TextToSpeechRequest(new ProviderText("text"), new LanguageId("russian"))));
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        using MicrosoftTextToSpeechProvider provider = new(new HttpClient());

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.ExecuteAsync(null!));
    }

    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new MicrosoftTextToSpeechProvider(null!));
    }

    [Fact]
    public void Dispose_Twice_DoesNotThrow()
    {
        MicrosoftTextToSpeechProvider provider = new(new HttpClient());

        provider.Dispose();

        Assert.Null(Record.Exception(provider.Dispose));
    }

    [LiveFact]
    public async Task ExecuteAsync_LiveMicrosoftTranslator_SpeaksText()
    {
        using MicrosoftTextToSpeechProvider provider = new();

        TextToSpeechRequest request = new(new ProviderText("Доброе утро"), new LanguageId("russian"));
        TextToSpeechResult result = await provider.ExecuteAsync(request);

        Assert.False(result.AudioData.IsEmpty);
        Assert.Equal("audio/mpeg", result.ContentType.Value);
    }
}
