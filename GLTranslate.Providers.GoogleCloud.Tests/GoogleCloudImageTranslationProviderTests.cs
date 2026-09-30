using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;
using System.Net;
using System.Text;
using System.Text.Json;

namespace GLTranslate.Providers.GoogleCloud.Tests;

/// <summary>
/// Verifies the image translation provider: what it asks of Vision, what it
/// asks of Translation, and how the two are put together.
/// </summary>
public sealed class GoogleCloudImageTranslationProviderTests
{
    private static readonly GoogleCloudCredentials Credentials = new("AIza-secret-key");

    private static readonly byte[] Image = [0x89, 0x50, 0x4E, 0x47];

    private static readonly string TwoLines = VisionAnswer.Page(
        "en",
        VisionAnswer.Word("Good", 10, 59, 87, 20, "SPACE"),
        VisionAnswer.Word("morning", 113, 59, 152, 20, "EOL_SURE_SPACE"),
        VisionAnswer.Word("Good", 10, 90, 87, 20, "SPACE"),
        VisionAnswer.Word("evening", 113, 90, 152, 20, "LINE_BREAK"));

    private const string Translated = """{"data":{"translations":[{"translatedText":"Доброе утро","detectedSourceLanguage":"en"},{"translatedText":"Добрый вечер","detectedSourceLanguage":"en"}]}}""";

    /// <summary>
    /// Plays Google Cloud: answers Vision with what was read and Translation
    /// with the translations, noting what it was asked.
    /// </summary>
    private sealed class GoogleCloud(string vision = "", string translation = Translated)
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        public List<string> Bodies { get; } = [];

        public HttpResponseMessage Answer(HttpRequestMessage request)
        {
            Requests.Add(request);
            Bodies.Add(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());

            bool isVision = request.RequestUri!.Host.StartsWith("vision", StringComparison.Ordinal);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    isVision ? vision.Length == 0 ? TwoLines : vision : translation,
                    Encoding.UTF8,
                    "application/json"),
            };
        }
    }

    [Fact]
    public void Name_IsGoogleCloud()
    {
        using GoogleCloudImageTranslationProvider provider = new(Credentials, new HttpClient());

        Assert.Equal("Google Cloud", provider.Name);
    }

    [Fact]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new GoogleCloudImageTranslationProvider(null!));
        Assert.Throws<ArgumentNullException>(() => new GoogleCloudImageTranslationProvider(Credentials, null!));
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        using GoogleCloudImageTranslationProvider provider = new(Credentials, new HttpClient());

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsync_Answer_ReturnsEveryLineWithItsPlaceAndItsWords()
    {
        GoogleCloud google = new();

        using StubHttpMessageHandler handler = new(google.Answer);
        using HttpClient httpClient = new(handler);
        using GoogleCloudImageTranslationProvider provider = new(Credentials, httpClient);

        ImageTranslationRequest request = new(new ProviderImage(Image, "image/png"), new LanguageId("russian"));
        ImageTranslationResult result = await provider.ExecuteAsync(request);

        Assert.Equal(2, result.Lines.Length);

        TranslatedLine first = result.Lines[0];

        Assert.Equal("Good morning", first.RecognizedText);
        Assert.Equal("Доброе утро", first.TranslatedText);
        Assert.Equal(new TextBounds(10, 59, 255, 20), first.Bounds);
        Assert.Equal(2, first.Words.Length);
        Assert.Equal("morning", first.Words[1].Text);
        Assert.Equal(new TextBounds(113, 59, 152, 20), first.Words[1].Bounds);

        Assert.Equal("Добрый вечер", result.Lines[1].TranslatedText);

        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.Equal("russian", result.TargetLanguageId.Value);
        Assert.True(result.WasSourceLanguageDetected);
        Assert.Equal(request.Id, result.RequestId);
    }

    [Fact]
    public async Task ExecuteAsync_AsksVisionForTheDocumentTextAndTranslationForAllLinesAtOnce()
    {
        GoogleCloud google = new();

        using StubHttpMessageHandler handler = new(google.Answer);
        using HttpClient httpClient = new(handler);
        using GoogleCloudImageTranslationProvider provider = new(Credentials, httpClient);

        await provider.ExecuteAsync(new ImageTranslationRequest(new ProviderImage(Image, "image/png"), new LanguageId("russian")));

        Assert.Equal(2, google.Requests.Count);
        Assert.Equal("https://vision.googleapis.com/v1/images:annotate", google.Requests[0].RequestUri!.AbsoluteUri);
        Assert.Equal("https://translation.googleapis.com/language/translate/v2", google.Requests[1].RequestUri!.AbsoluteUri);

        using JsonDocument vision = JsonDocument.Parse(google.Bodies[0]);
        JsonElement asked = vision.RootElement.GetProperty("requests")[0];

        Assert.Equal(Convert.ToBase64String(Image), asked.GetProperty("image").GetProperty("content").GetString());
        Assert.Equal("DOCUMENT_TEXT_DETECTION", asked.GetProperty("features")[0].GetProperty("type").GetString());
        Assert.False(asked.TryGetProperty("imageContext", out _));

        using JsonDocument translation = JsonDocument.Parse(google.Bodies[1]);

        Assert.Equal(
            ["Good morning", "Good evening"],
            translation.RootElement.GetProperty("q").EnumerateArray().Select(piece => piece.GetString()));
        Assert.Equal("text", translation.RootElement.GetProperty("format").GetString());
    }

    [Fact]
    public async Task ExecuteAsync_WithSourceLanguage_HintsVisionAndKeepsIt()
    {
        GoogleCloud google = new();

        using StubHttpMessageHandler handler = new(google.Answer);
        using HttpClient httpClient = new(handler);
        using GoogleCloudImageTranslationProvider provider = new(Credentials, httpClient);

        ImageTranslationResult result = await provider.ExecuteAsync(new ImageTranslationRequest(
            new ProviderImage(Image, "image/png"),
            new LanguageId("russian"),
            new LanguageId("english")));

        using JsonDocument vision = JsonDocument.Parse(google.Bodies[0]);
        JsonElement hints = vision.RootElement.GetProperty("requests")[0].GetProperty("imageContext").GetProperty("languageHints");

        Assert.Equal("en", Assert.Single(hints.EnumerateArray()).GetString());

        using JsonDocument translation = JsonDocument.Parse(google.Bodies[1]);

        Assert.Equal("en", translation.RootElement.GetProperty("source").GetString());
        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.False(result.WasSourceLanguageDetected);
    }

    [Fact]
    public async Task ExecuteAsync_NothingOnTheImage_ReturnsNoLinesAndAsksNothingOfTranslation()
    {
        GoogleCloud google = new(vision: """{"responses":[{}]}""");

        using StubHttpMessageHandler handler = new(google.Answer);
        using HttpClient httpClient = new(handler);
        using GoogleCloudImageTranslationProvider provider = new(Credentials, httpClient);

        ImageTranslationResult result = await provider.ExecuteAsync(
            new ImageTranslationRequest(new ProviderImage(Image, "image/png"), new LanguageId("russian")));

        Assert.Empty(result.Lines);
        Assert.Single(google.Requests);

        // Vision named nothing, so the language asked for is all there is to report.
        Assert.Equal("russian", result.SourceLanguageId.Value);
    }

    [Fact]
    public async Task ExecuteAsync_NoTextButALanguageSuspected_ReportsIt()
    {
        GoogleCloud google = new(vision: """{"responses":[{"fullTextAnnotation":{"pages":[{"property":{"detectedLanguages":[{"languageCode":"de"}]}}]}}]}""");

        using StubHttpMessageHandler handler = new(google.Answer);
        using HttpClient httpClient = new(handler);
        using GoogleCloudImageTranslationProvider provider = new(Credentials, httpClient);

        ImageTranslationResult result = await provider.ExecuteAsync(
            new ImageTranslationRequest(new ProviderImage(Image, "image/png"), new LanguageId("russian")));

        Assert.Equal("german", result.SourceLanguageId.Value);
    }

    [Fact]
    public async Task ExecuteAsync_VisionFailsOnTheImage_ThrowsProviderException()
    {
        GoogleCloud google = new(vision: """{"responses":[{"error":{"code":3,"message":"Bad image data."}}]}""");

        using StubHttpMessageHandler handler = new(google.Answer);
        using HttpClient httpClient = new(handler);
        using GoogleCloudImageTranslationProvider provider = new(Credentials, httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new ImageTranslationRequest(new ProviderImage(Image, "image/png"), new LanguageId("russian"))));

        Assert.Contains("Bad image data", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_VisionRefusesTheRequest_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("""{"error":{"code":403,"message":"Cloud Vision API has not been used."}}""", Encoding.UTF8, "application/json"),
        });
        using HttpClient httpClient = new(handler);
        using GoogleCloudImageTranslationProvider provider = new(Credentials, httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new ImageTranslationRequest(new ProviderImage(Image, "image/png"), new LanguageId("russian"))));

        Assert.Contains("Cloud Vision API", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_AnswerWithNoResponses_ThrowsProviderException()
    {
        GoogleCloud google = new(vision: """{"responses":[]}""");

        using StubHttpMessageHandler handler = new(google.Answer);
        using HttpClient httpClient = new(handler);
        using GoogleCloudImageTranslationProvider provider = new(Credentials, httpClient);

        await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new ImageTranslationRequest(new ProviderImage(Image, "image/png"), new LanguageId("russian"))));
    }
}
