using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.TextToSpeech;
using System.Net;
using System.Text;
using System.Text.Json;

namespace GLTranslate.Providers.GoogleCloud.Tests;

/// <summary>
/// Verifies the text-to-speech provider: how it picks a voice and what it asks
/// Google Cloud to say.
/// </summary>
public sealed class GoogleCloudTextToSpeechProviderTests
{
    private static readonly GoogleCloudCredentials Credentials = new("AIza-secret-key");

    private static readonly byte[] Mp3 = Encoding.UTF8.GetBytes("ID3-speech");

    private static readonly string Synthesized = $$"""{"audioContent":"{{Convert.ToBase64String(Mp3)}}"}""";

    private const string Voices = """{"voices":[{"languageCodes":["en-US"],"name":"en-US-Neural2-A"},{"languageCodes":["en-GB"],"name":"en-GB-Neural2-A"}]}""";

    /// <summary>
    /// Plays Google Cloud: lists voices and synthesizes, noting what it was asked.
    /// </summary>
    private sealed class GoogleCloud(string voices = Voices, string synthesized = "")
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        public List<string> Bodies { get; } = [];

        public HttpResponseMessage Answer(HttpRequestMessage request)
        {
            Requests.Add(request);
            Bodies.Add(request.Content is null ? string.Empty : request.Content.ReadAsStringAsync().GetAwaiter().GetResult());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    request.Method == HttpMethod.Get ? voices : synthesized.Length == 0 ? Synthesized : synthesized,
                    Encoding.UTF8,
                    "application/json"),
            };
        }
    }

    [Fact]
    public void Name_IsGoogleCloud()
    {
        using GoogleCloudTextToSpeechProvider provider = new(Credentials, new HttpClient());

        Assert.Equal("Google Cloud", provider.Name);
    }

    [Fact]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new GoogleCloudTextToSpeechProvider(null!));
        Assert.Throws<ArgumentNullException>(() => new GoogleCloudTextToSpeechProvider(Credentials, null!));
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        using GoogleCloudTextToSpeechProvider provider = new(Credentials, new HttpClient());

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsync_NoVoice_AsksForTheRegionOfTheLanguageAndSpeaksIt()
    {
        GoogleCloud google = new();

        using StubHttpMessageHandler handler = new(google.Answer);
        using HttpClient httpClient = new(handler);
        using GoogleCloudTextToSpeechProvider provider = new(Credentials, httpClient);

        TextToSpeechRequest request = new(new ProviderText("Hello"), new LanguageId("english"));
        TextToSpeechResult result = await provider.ExecuteAsync(request);

        Assert.Equal(2, google.Requests.Count);
        Assert.Equal(
            "https://texttospeech.googleapis.com/v1/voices?languageCode=en",
            google.Requests[0].RequestUri!.AbsoluteUri);
        Assert.Equal("https://texttospeech.googleapis.com/v1/text:synthesize", google.Requests[1].RequestUri!.AbsoluteUri);

        using JsonDocument json = JsonDocument.Parse(google.Bodies[1]);
        JsonElement root = json.RootElement;

        Assert.Equal("Hello", root.GetProperty("input").GetProperty("text").GetString());
        Assert.Equal("en-US", root.GetProperty("voice").GetProperty("languageCode").GetString());
        Assert.False(root.GetProperty("voice").TryGetProperty("name", out _));
        Assert.Equal("MP3", root.GetProperty("audioConfig").GetProperty("audioEncoding").GetString());

        Assert.Equal(Mp3, result.AudioData.ToArray());
        Assert.Equal("audio/mpeg", result.ContentType.Value);
        Assert.Equal("english", result.LanguageId.Value);
        Assert.Equal(request.Id, result.RequestId);
        Assert.All(google.Requests, r => Assert.Equal("AIza-secret-key", Assert.Single(r.Headers.GetValues("X-Goog-Api-Key"))));
    }

    [Fact]
    public async Task ExecuteAsync_SameLanguageTwice_AsksForItsRegionOnce()
    {
        GoogleCloud google = new();

        using StubHttpMessageHandler handler = new(google.Answer);
        using HttpClient httpClient = new(handler);
        using GoogleCloudTextToSpeechProvider provider = new(Credentials, httpClient);

        await provider.ExecuteAsync(new TextToSpeechRequest(new ProviderText("One"), new LanguageId("english")));
        await provider.ExecuteAsync(new TextToSpeechRequest(new ProviderText("Two"), new LanguageId("english")));

        // One question about the language and two recordings.
        Assert.Equal(1, google.Requests.Count(request => request.Method == HttpMethod.Get));
        Assert.Equal(2, google.Requests.Count(request => request.Method == HttpMethod.Post));
    }

    [Fact]
    public async Task ExecuteAsync_NamedVoice_SpeaksItWithoutAskingForRegions()
    {
        GoogleCloud google = new();

        using StubHttpMessageHandler handler = new(google.Answer);
        using HttpClient httpClient = new(handler);
        using GoogleCloudTextToSpeechProvider provider = new(Credentials, httpClient);

        await provider.ExecuteAsync(new TextToSpeechRequest(
            new ProviderText("Hello"),
            new LanguageId("english"),
            new VoiceName("en-GB-Neural2-A")));

        HttpRequestMessage only = Assert.Single(google.Requests);

        Assert.Equal(HttpMethod.Post, only.Method);

        using JsonDocument json = JsonDocument.Parse(google.Bodies[0]);

        Assert.Equal("en-GB", json.RootElement.GetProperty("voice").GetProperty("languageCode").GetString());
        Assert.Equal("en-GB-Neural2-A", json.RootElement.GetProperty("voice").GetProperty("name").GetString());
    }

    [Theory]
    [InlineData("Alice")]
    [InlineData("en-US")]
    [InlineData("-US-A")]
    public async Task ExecuteAsync_VoiceNameThatIsNotGoogles_ThrowsProviderExceptionBeforeAnyRequest(string name)
    {
        GoogleCloud google = new();

        using StubHttpMessageHandler handler = new(google.Answer);
        using HttpClient httpClient = new(handler);
        using GoogleCloudTextToSpeechProvider provider = new(Credentials, httpClient);

        await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(new TextToSpeechRequest(
            new ProviderText("Hello"),
            new LanguageId("english"),
            new VoiceName(name))));

        Assert.Empty(google.Requests);
    }

    [Fact]
    public async Task ExecuteAsync_TextOver5000Bytes_ThrowsProviderExceptionBeforeAnyRequest()
    {
        GoogleCloud google = new();

        using StubHttpMessageHandler handler = new(google.Answer);
        using HttpClient httpClient = new(handler);
        using GoogleCloudTextToSpeechProvider provider = new(Credentials, httpClient);

        // 2,501 Cyrillic letters are 5,002 bytes though far fewer than 5,000 characters.
        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new TextToSpeechRequest(new ProviderText(new string('я', 2501)), new LanguageId("russian"))));

        Assert.Contains("5000", exception.Message, StringComparison.Ordinal);
        Assert.Empty(google.Requests);
    }

    [Fact]
    public async Task ExecuteAsync_NoVoiceSpeaksTheLanguage_ThrowsProviderException()
    {
        GoogleCloud google = new(voices: "{}");

        using StubHttpMessageHandler handler = new(google.Answer);
        using HttpClient httpClient = new(handler);
        using GoogleCloudTextToSpeechProvider provider = new(Credentials, httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new TextToSpeechRequest(new ProviderText("Hello"), new LanguageId("english"))));

        Assert.Contains("no voice", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_AnswerWithoutAudio_ThrowsProviderException()
    {
        GoogleCloud google = new(synthesized: "{}");

        using StubHttpMessageHandler handler = new(google.Answer);
        using HttpClient httpClient = new(handler);
        using GoogleCloudTextToSpeechProvider provider = new(Credentials, httpClient);

        await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new TextToSpeechRequest(new ProviderText("Hello"), new LanguageId("english"))));
    }

    [Fact]
    public async Task ExecuteAsync_AudioThatIsNotBase64_ThrowsProviderException()
    {
        GoogleCloud google = new(synthesized: """{"audioContent":"***"}""");

        using StubHttpMessageHandler handler = new(google.Answer);
        using HttpClient httpClient = new(handler);
        using GoogleCloudTextToSpeechProvider provider = new(Credentials, httpClient);

        await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new TextToSpeechRequest(new ProviderText("Hello"), new LanguageId("english"), new VoiceName("en-US-Neural2-A"))));
    }

    [Fact]
    public async Task ExecuteAsync_RefusedByGoogle_ThrowsProviderExceptionWithItsExplanation()
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("""{"error":{"code":403,"message":"Text-to-Speech API has not been used."}}""", Encoding.UTF8, "application/json"),
        });
        using HttpClient httpClient = new(handler);
        using GoogleCloudTextToSpeechProvider provider = new(Credentials, httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new TextToSpeechRequest(new ProviderText("Hello"), new LanguageId("english"), new VoiceName("en-US-Neural2-A"))));

        Assert.Contains("has not been used", exception.Message, StringComparison.Ordinal);
    }
}
