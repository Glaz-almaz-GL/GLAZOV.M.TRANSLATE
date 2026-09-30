using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.TextToSpeech;
using System.Net;
using System.Text;
using System.Web;

namespace GLTranslate.Providers.YandexCloud.Tests;

/// <summary>
/// Verifies the text-to-speech provider: how it picks a language and a voice
/// and what it asks SpeechKit to say.
/// </summary>
public sealed class YandexCloudTextToSpeechProviderTests
{
    private static readonly YandexCloudCredentials Credentials = new("AQVN-secret-key");

    private static readonly byte[] Mp3 = Encoding.UTF8.GetBytes("ID3-speech");

    private static StubHttpMessageHandler Answering(
        byte[]? audio = null,
        HttpStatusCode status = HttpStatusCode.OK,
        Action<HttpRequestMessage, string>? observe = null)
    {
        return new StubHttpMessageHandler(request =>
        {
            observe?.Invoke(request, request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());

            return new HttpResponseMessage(status) { Content = new ByteArrayContent(audio ?? Mp3) };
        });
    }

    [Fact]
    public void Name_IsYandexCloud()
    {
        using YandexCloudTextToSpeechProvider provider = new(Credentials, new HttpClient());

        Assert.Equal("Yandex Cloud", provider.Name);
    }

    [Fact]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new YandexCloudTextToSpeechProvider(null!));
        Assert.Throws<ArgumentNullException>(() => new YandexCloudTextToSpeechProvider(Credentials, null!));
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        using YandexCloudTextToSpeechProvider provider = new(Credentials, new HttpClient());

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.ExecuteAsync(null!));
    }

    [Theory]
    [InlineData("russian", "ru-RU", "marina")]
    [InlineData("english", "en-US", "john")]
    [InlineData("german", "de-DE", "lea")]
    public async Task ExecuteAsync_NoVoice_SpeaksTheDefaultVoiceOfTheLanguage(string languageId, string lang, string voice)
    {
        HttpRequestMessage? sent = null;
        string? body = null;

        using StubHttpMessageHandler handler = Answering(observe: (request, content) =>
        {
            sent = request;
            body = content;
        });
        using HttpClient httpClient = new(handler);
        using YandexCloudTextToSpeechProvider provider = new(Credentials, httpClient);

        TextToSpeechRequest request = new(new ProviderText("Hello"), new LanguageId(languageId));
        TextToSpeechResult result = await provider.ExecuteAsync(request);

        Assert.Equal("https://tts.api.cloud.yandex.net/speech/v1/tts:synthesize", sent!.RequestUri!.AbsoluteUri);
        Assert.Equal("Api-Key AQVN-secret-key", sent.Headers.Authorization!.ToString());

        var fields = HttpUtility.ParseQueryString(body!);

        Assert.Equal("Hello", fields["text"]);
        Assert.Equal(lang, fields["lang"]);
        Assert.Equal(voice, fields["voice"]);
        Assert.Equal("mp3", fields["format"]);
        Assert.Null(fields["folderId"]);

        Assert.Equal(Mp3, result.AudioData.ToArray());
        Assert.Equal("audio/mpeg", result.ContentType.Value);
        Assert.Equal(languageId, result.LanguageId.Value);
        Assert.Equal(request.Id, result.RequestId);
    }

    [Fact]
    public async Task ExecuteAsync_NamedVoice_SpeaksIt()
    {
        string? body = null;

        using StubHttpMessageHandler handler = Answering(observe: (_, content) => body = content);
        using HttpClient httpClient = new(handler);
        using YandexCloudTextToSpeechProvider provider = new(Credentials, httpClient);

        await provider.ExecuteAsync(new TextToSpeechRequest(
            new ProviderText("Привет"),
            new LanguageId("russian"),
            new VoiceName("filipp")));

        var fields = HttpUtility.ParseQueryString(body!);

        Assert.Equal("filipp", fields["voice"]);
        Assert.Equal("ru-RU", fields["lang"]);
        Assert.Equal("Привет", fields["text"]);
    }

    [Fact]
    public async Task ExecuteAsync_CredentialsWithFolder_SendItInTheForm()
    {
        string? body = null;

        using StubHttpMessageHandler handler = Answering(observe: (_, content) => body = content);
        using HttpClient httpClient = new(handler);
        using YandexCloudTextToSpeechProvider provider = new(new YandexCloudCredentials("key", "b1g123"), httpClient);

        await provider.ExecuteAsync(new TextToSpeechRequest(new ProviderText("Hello"), new LanguageId("english")));

        Assert.Equal("b1g123", HttpUtility.ParseQueryString(body!)["folderId"]);
    }

    [Fact]
    public async Task ExecuteAsync_LanguageSpeechKitDoesNotSpeak_ThrowsProviderExceptionBeforeAnyRequest()
    {
        bool sent = false;

        using StubHttpMessageHandler handler = Answering(observe: (_, _) => sent = true);
        using HttpClient httpClient = new(handler);
        using YandexCloudTextToSpeechProvider provider = new(Credentials, httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new TextToSpeechRequest(new ProviderText("Bonjour"), new LanguageId("french"))));

        Assert.Contains("french", exception.Message, StringComparison.Ordinal);
        Assert.False(sent);
    }

    [Fact]
    public async Task ExecuteAsync_TextOver5000Characters_ThrowsProviderExceptionBeforeAnyRequest()
    {
        bool sent = false;

        using StubHttpMessageHandler handler = Answering(observe: (_, _) => sent = true);
        using HttpClient httpClient = new(handler);
        using YandexCloudTextToSpeechProvider provider = new(Credentials, httpClient);

        await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new TextToSpeechRequest(new ProviderText(new string('a', 5001)), new LanguageId("english"))));

        Assert.False(sent);
    }

    [Fact]
    public async Task ExecuteAsync_TextThatFitsInCharactersButNotInTheEncodedForm_ThrowsProviderException()
    {
        bool sent = false;

        using StubHttpMessageHandler handler = Answering(observe: (_, _) => sent = true);
        using HttpClient httpClient = new(handler);
        using YandexCloudTextToSpeechProvider provider = new(Credentials, httpClient);

        // 3,000 Cyrillic letters are far under 5,000 characters, but encoded
        // they take 18,000 bytes of a form that may take 15 KB.
        await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new TextToSpeechRequest(new ProviderText(new string('я', 3000)), new LanguageId("russian"))));

        Assert.False(sent);
    }

    [Fact]
    public async Task ExecuteAsync_AnswerWithoutAudio_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering(audio: []);
        using HttpClient httpClient = new(handler);
        using YandexCloudTextToSpeechProvider provider = new(Credentials, httpClient);

        await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new TextToSpeechRequest(new ProviderText("Hello"), new LanguageId("english"))));
    }

    [Fact]
    public async Task ExecuteAsync_RefusedByYandex_ThrowsProviderExceptionWithItsExplanation()
    {
        using StubHttpMessageHandler handler = Answering(
            Encoding.UTF8.GetBytes("""{"code":7,"message":"SpeechKit is not enabled for the folder."}"""),
            HttpStatusCode.Forbidden);
        using HttpClient httpClient = new(handler);
        using YandexCloudTextToSpeechProvider provider = new(Credentials, httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new TextToSpeechRequest(new ProviderText("Hello"), new LanguageId("english"))));

        Assert.Contains("403", exception.Message, StringComparison.Ordinal);
        Assert.Contains("not enabled", exception.Message, StringComparison.Ordinal);
    }
}
