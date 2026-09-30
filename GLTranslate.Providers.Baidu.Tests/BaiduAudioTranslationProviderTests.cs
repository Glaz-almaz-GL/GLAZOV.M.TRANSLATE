using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.TextToSpeech;
using GLTranslate.Abstractions.Translation;
using System.Net;
using System.Text;
using System.Text.Json;

namespace GLTranslate.Providers.Baidu.Tests;

/// <summary>
/// Verifies the audio translation provider: what it sends of a recording and
/// what it makes of what Baidu heard.
/// </summary>
public sealed class BaiduAudioTranslationProviderTests
{
    private static readonly BaiduCredentials Credentials = new("2015063000000001", "1234567890");

    private static readonly byte[] Speech = [0x52, 0x49, 0x46, 0x46, 0x01, 0x02];

    private static readonly byte[] Spoken = Encoding.UTF8.GetBytes("ID3-translated-speech");

    private static readonly string Answer = $$$"""
        {"code":0,"msg":"Success","data":{"source":"今天天气不错。","target":"It's a nice day today.","target_tts":"{{{Convert.ToBase64String(Spoken)}}}"}}
        """;

    private static StubHttpMessageHandler Answering(
        string json = "",
        Action<HttpRequestMessage, string>? observe = null)
    {
        return new StubHttpMessageHandler(request =>
        {
            observe?.Invoke(request, request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json.Length == 0 ? Answer : json, Encoding.UTF8, "application/json"),
            };
        });
    }

    private static AudioTranslationRequest Request(string contentType = "audio/wav", LanguageId? source = null)
    {
        return new AudioTranslationRequest(
            new ProviderAudio(Speech, new AudioContentType(contentType)),
            new LanguageId("english"),
            source ?? new LanguageId("chinese"));
    }

    [Fact]
    public void Name_IsBaidu()
    {
        using BaiduAudioTranslationProvider provider = new(Credentials, new HttpClient());

        Assert.Equal("Baidu", provider.Name);
    }

    [Fact]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new BaiduAudioTranslationProvider(null!));
        Assert.Throws<ArgumentNullException>(() => new BaiduAudioTranslationProvider(Credentials, null!));
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        using BaiduAudioTranslationProvider provider = new(Credentials, new HttpClient());

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsync_Answer_ReturnsWhatWasHeardTranslatedAndSpoken()
    {
        using StubHttpMessageHandler handler = Answering();
        using HttpClient httpClient = new(handler);
        using BaiduAudioTranslationProvider provider = new(Credentials, httpClient);

        AudioTranslationRequest request = Request();
        AudioTranslationResult result = await provider.ExecuteAsync(request);

        Assert.Equal("今天天气不错。", result.RecognizedText.Value);
        Assert.Equal("It's a nice day today.", result.TranslatedText.Value);

        SpokenTranslation speech = Assert.NotNull(result.Speech);

        Assert.Equal(Spoken, speech.AudioData.ToArray());
        Assert.Equal("audio/mpeg", speech.ContentType.Value);

        Assert.Equal("chinese", result.SourceLanguageId.Value);
        Assert.Equal("english", result.TargetLanguageId.Value);
        Assert.False(result.WasSourceLanguageDetected);
        Assert.Equal(request.Id, result.RequestId);
    }

    [Fact]
    public async Task ExecuteAsync_SendsTheRecordingSignedByItsEncodedAudio()
    {
        HttpRequestMessage? sent = null;
        string? body = null;

        using StubHttpMessageHandler handler = Answering(observe: (request, content) =>
        {
            sent = request;
            body = content;
        });
        using HttpClient httpClient = new(handler);
        using BaiduAudioTranslationProvider provider = new(Credentials, httpClient);

        await provider.ExecuteAsync(Request());

        Assert.Equal("https://fanyi-api.baidu.com/api/trans/v2/voicetrans", sent!.RequestUri!.AbsoluteUri);

        using JsonDocument json = JsonDocument.Parse(body!);
        string voice = Convert.ToBase64String(Speech);

        Assert.Equal("zh", json.RootElement.GetProperty("from").GetString());
        Assert.Equal("en", json.RootElement.GetProperty("to").GetString());
        Assert.Equal("wav", json.RootElement.GetProperty("format").GetString());
        Assert.Equal(voice, json.RootElement.GetProperty("voice").GetString());

        // The signature covers the application, the time and the encoded audio,
        // not the whole body.
        string timestamp = Assert.Single(sent.Headers.GetValues("X-Timestamp"));

        Assert.Equal("2015063000000001", Assert.Single(sent.Headers.GetValues("X-Appid")));
        Assert.Equal(
            Internal.BaiduSignature.ForHeaders("2015063000000001", timestamp, voice, "1234567890"),
            Assert.Single(sent.Headers.GetValues("X-Sign")));
    }

    [Theory]
    [InlineData("audio/wav", "wav")]
    [InlineData("audio/x-wav", "wav")]
    [InlineData("audio/amr", "amr")]
    [InlineData("audio/mp4", "m4a")]
    [InlineData("audio/x-m4a", "m4a")]
    [InlineData("audio/L16;rate=16000", "pcm")]
    [InlineData("audio/pcm", "pcm")]
    public async Task ExecuteAsync_ContentType_IsSentAsTheFormatBaiduNames(string contentType, string format)
    {
        string? body = null;

        using StubHttpMessageHandler handler = Answering(observe: (_, content) => body = content);
        using HttpClient httpClient = new(handler);
        using BaiduAudioTranslationProvider provider = new(Credentials, httpClient);

        await provider.ExecuteAsync(Request(contentType));

        using JsonDocument json = JsonDocument.Parse(body!);

        Assert.Equal(format, json.RootElement.GetProperty("format").GetString());
    }

    [Fact]
    public async Task ExecuteAsync_NoSourceLanguage_ThrowsBeforeAnyRequest()
    {
        bool sent = false;

        using StubHttpMessageHandler handler = Answering(observe: (_, _) => sent = true);
        using HttpClient httpClient = new(handler);
        using BaiduAudioTranslationProvider provider = new(Credentials, httpClient);

        AudioTranslationRequest request = new(
            new ProviderAudio(Speech, new AudioContentType("audio/wav")),
            new LanguageId("english"));

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(request));

        Assert.Contains("source language", exception.Message, StringComparison.Ordinal);
        Assert.False(sent);
    }

    [Fact]
    public async Task ExecuteAsync_ContentTypeBaiduDoesNotHear_ThrowsBeforeAnyRequest()
    {
        bool sent = false;

        using StubHttpMessageHandler handler = Answering(observe: (_, _) => sent = true);
        using HttpClient httpClient = new(handler);
        using BaiduAudioTranslationProvider provider = new(Credentials, httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(Request("audio/mpeg")));

        Assert.Contains("audio/mpeg", exception.Message, StringComparison.Ordinal);
        Assert.False(sent);
    }

    [Fact]
    public async Task ExecuteAsync_RecordingOverFourMegabytes_ThrowsBeforeAnyRequest()
    {
        bool sent = false;

        using StubHttpMessageHandler handler = Answering(observe: (_, _) => sent = true);
        using HttpClient httpClient = new(handler);
        using BaiduAudioTranslationProvider provider = new(Credentials, httpClient);

        AudioTranslationRequest request = new(
            new ProviderAudio(new byte[(4 * 1024 * 1024) + 1], new AudioContentType("audio/wav")),
            new LanguageId("english"),
            new LanguageId("chinese"));

        await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(request));

        Assert.False(sent);
    }

    [Fact]
    public async Task ExecuteAsync_AnswerWithoutSpeech_ReturnsNullSpeech()
    {
        using StubHttpMessageHandler handler = Answering(
            """{"code":0,"msg":"Success","data":{"source":"你好","target":"Hello"}}""");
        using HttpClient httpClient = new(handler);
        using BaiduAudioTranslationProvider provider = new(Credentials, httpClient);

        AudioTranslationResult result = await provider.ExecuteAsync(Request());

        Assert.Null(result.Speech);
        Assert.Equal("Hello", result.TranslatedText.Value);
    }

    [Fact]
    public async Task ExecuteAsync_SpeechThatIsNotBase64_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering(
            """{"code":0,"msg":"Success","data":{"source":"你好","target":"Hello","target_tts":"***"}}""");
        using HttpClient httpClient = new(handler);
        using BaiduAudioTranslationProvider provider = new(Credentials, httpClient);

        await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(Request()));
    }

    [Theory]
    [InlineData("""{"code":20202,"msg":"file format not supported"}""", "20202")]
    [InlineData("""{"code":10006,"msg":"unauthorized"}""", "10006")]
    public async Task ExecuteAsync_RefusedByBaidu_ThrowsProviderExceptionNamingTheCode(string json, string code)
    {
        using StubHttpMessageHandler handler = Answering(json);
        using HttpClient httpClient = new(handler);
        using BaiduAudioTranslationProvider provider = new(Credentials, httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(Request()));

        Assert.Equal("Baidu", exception.ProviderName);
        Assert.Contains(code, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_HeardNothing_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering("""{"code":0,"msg":"Success","data":{}}""");
        using HttpClient httpClient = new(handler);
        using BaiduAudioTranslationProvider provider = new(Credentials, httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(Request()));

        Assert.Contains("nothing", exception.Message, StringComparison.Ordinal);
    }
}
