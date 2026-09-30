using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;
using System.Net;
using System.Text;
using System.Text.Json;

namespace GLTranslate.Providers.YandexCloud.Tests;

/// <summary>
/// Verifies the image translation provider: what it asks of Vision OCR, what
/// it asks of Translate, and how the two are put together.
/// </summary>
public sealed class YandexCloudImageTranslationProviderTests
{
    private const string Recognized = """
        {"textAnnotation":{"width":"400","height":"120","blocks":[
        {"languages":[{"languageCode":"en"}],"lines":[
        {"text":"Good morning","boundingBox":{"vertices":[{"x":"10","y":"59"},{"x":"384","y":"59"},{"x":"384","y":"79"},{"x":"10","y":"79"}]},
         "words":[
         {"text":"Good","boundingBox":{"vertices":[{"x":"10","y":"55"},{"x":"97"},{"x":"97","y":"83"},{"x":"10","y":"83"}]}},
         {"text":"morning","boundingBox":{"vertices":[{"x":"113","y":"55"},{"x":"265","y":"55"},{"x":"265","y":"83"},{"x":"113","y":"83"}]}}]},
        {"text":"Good evening","boundingBox":{"vertices":[{"x":"10","y":"90"},{"x":"310","y":"90"},{"x":"310","y":"110"},{"x":"10","y":"110"}]}}]}]},"page":"0"}
        """;

    private const string Translated = """{"translations":[{"text":"Доброе утро","detectedLanguageCode":"en"},{"text":"Добрый вечер","detectedLanguageCode":"en"}]}""";

    private static readonly YandexCloudCredentials Credentials = new("AQVN-secret-key");

    private static readonly byte[] Image = [0x89, 0x50, 0x4E, 0x47];

    /// <summary>
    /// Plays Yandex Cloud: answers OCR with what was read and Translate with the
    /// translations, noting what it was asked.
    /// </summary>
    private sealed class YandexCloud(string ocr = Recognized, string translation = Translated)
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        public List<string> Bodies { get; } = [];

        public HttpResponseMessage Answer(HttpRequestMessage request)
        {
            Requests.Add(request);
            Bodies.Add(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    request.RequestUri!.Host.StartsWith("ocr", StringComparison.Ordinal) ? ocr : translation,
                    Encoding.UTF8,
                    "application/json"),
            };
        }
    }

    [Fact]
    public void Name_IsYandexCloud()
    {
        using YandexCloudImageTranslationProvider provider = new(Credentials, new HttpClient());

        Assert.Equal("Yandex Cloud", provider.Name);
    }

    [Fact]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new YandexCloudImageTranslationProvider(null!));
        Assert.Throws<ArgumentNullException>(() => new YandexCloudImageTranslationProvider(Credentials, null!));
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        using YandexCloudImageTranslationProvider provider = new(Credentials, new HttpClient());

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsync_Answer_ReturnsEveryLineWithItsPlaceAndItsWords()
    {
        YandexCloud yandex = new();

        using StubHttpMessageHandler handler = new(yandex.Answer);
        using HttpClient httpClient = new(handler);
        using YandexCloudImageTranslationProvider provider = new(Credentials, httpClient);

        ImageTranslationRequest request = new(new ProviderImage(Image, "image/png"), new LanguageId("russian"));
        ImageTranslationResult result = await provider.ExecuteAsync(request);

        Assert.Equal(2, result.Lines.Length);

        TranslatedLine first = result.Lines[0];

        Assert.Equal("Good morning", first.RecognizedText);
        Assert.Equal("Доброе утро", first.TranslatedText);
        Assert.Equal(new TextBounds(10, 59, 374, 20), first.Bounds);
        Assert.Equal(2, first.Words.Length);
        Assert.Equal("morning", first.Words[1].Text);
        Assert.Equal(new TextBounds(113, 55, 152, 28), first.Words[1].Bounds);

        Assert.Equal("Добрый вечер", result.Lines[1].TranslatedText);
        Assert.Empty(result.Lines[1].Words);

        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.Equal("russian", result.TargetLanguageId.Value);
        Assert.True(result.WasSourceLanguageDetected);
        Assert.Equal(request.Id, result.RequestId);
    }

    [Fact]
    public async Task ExecuteAsync_AsksOcrForAllLanguagesAndNotToKeepTheImage()
    {
        YandexCloud yandex = new();

        using StubHttpMessageHandler handler = new(yandex.Answer);
        using HttpClient httpClient = new(handler);
        using YandexCloudImageTranslationProvider provider = new(Credentials, httpClient);

        await provider.ExecuteAsync(new ImageTranslationRequest(new ProviderImage(Image, "image/png"), new LanguageId("russian")));

        Assert.Equal(2, yandex.Requests.Count);

        HttpRequestMessage ocr = yandex.Requests[0];

        Assert.Equal("https://ocr.api.cloud.yandex.net/ocr/v1/recognizeText", ocr.RequestUri!.AbsoluteUri);
        Assert.Equal("Api-Key AQVN-secret-key", ocr.Headers.Authorization!.ToString());
        Assert.Equal("false", Assert.Single(ocr.Headers.GetValues("x-data-logging-enabled")));
        Assert.False(ocr.Headers.Contains("x-folder-id"));

        using JsonDocument body = JsonDocument.Parse(yandex.Bodies[0]);

        Assert.Equal(Convert.ToBase64String(Image), body.RootElement.GetProperty("content").GetString());
        Assert.Equal("image/png", body.RootElement.GetProperty("mimeType").GetString());
        Assert.Equal("*", Assert.Single(body.RootElement.GetProperty("languageCodes").EnumerateArray()).GetString());
        Assert.Equal("page", body.RootElement.GetProperty("model").GetString());
    }

    [Fact]
    public async Task ExecuteAsync_TranslatesAllLinesInOneRequest()
    {
        YandexCloud yandex = new();

        using StubHttpMessageHandler handler = new(yandex.Answer);
        using HttpClient httpClient = new(handler);
        using YandexCloudImageTranslationProvider provider = new(Credentials, httpClient);

        await provider.ExecuteAsync(new ImageTranslationRequest(new ProviderImage(Image, "image/png"), new LanguageId("russian")));

        Assert.Equal("https://translate.api.cloud.yandex.net/translate/v2/translate", yandex.Requests[1].RequestUri!.AbsoluteUri);

        using JsonDocument body = JsonDocument.Parse(yandex.Bodies[1]);

        Assert.Equal(
            ["Good morning", "Good evening"],
            body.RootElement.GetProperty("texts").EnumerateArray().Select(piece => piece.GetString()));
    }

    [Fact]
    public async Task ExecuteAsync_WithSourceLanguageAndFolder_HintsOcrAndSendsTheFolder()
    {
        YandexCloud yandex = new();

        using StubHttpMessageHandler handler = new(yandex.Answer);
        using HttpClient httpClient = new(handler);
        using YandexCloudImageTranslationProvider provider = new(new YandexCloudCredentials("key", "b1g123"), httpClient);

        ImageTranslationResult result = await provider.ExecuteAsync(new ImageTranslationRequest(
            new ProviderImage(Image, "image/jpeg"),
            new LanguageId("russian"),
            new LanguageId("english")));

        Assert.Equal("b1g123", Assert.Single(yandex.Requests[0].Headers.GetValues("x-folder-id")));

        using JsonDocument body = JsonDocument.Parse(yandex.Bodies[0]);

        Assert.Equal("en", Assert.Single(body.RootElement.GetProperty("languageCodes").EnumerateArray()).GetString());
        Assert.Equal("image/jpeg", body.RootElement.GetProperty("mimeType").GetString());
        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.False(result.WasSourceLanguageDetected);
    }

    [Fact]
    public async Task ExecuteAsync_AnswerWrappedInResult_IsReadAllTheSame()
    {
        YandexCloud yandex = new(ocr: "{\"result\":" + Recognized + "}");

        using StubHttpMessageHandler handler = new(yandex.Answer);
        using HttpClient httpClient = new(handler);
        using YandexCloudImageTranslationProvider provider = new(Credentials, httpClient);

        ImageTranslationResult result = await provider.ExecuteAsync(
            new ImageTranslationRequest(new ProviderImage(Image, "image/png"), new LanguageId("russian")));

        Assert.Equal(2, result.Lines.Length);
    }

    [Fact]
    public async Task ExecuteAsync_AnswerOfSeveralJsonValues_ReadsTheFirst()
    {
        YandexCloud yandex = new(ocr: Recognized + "\n" + Recognized);

        using StubHttpMessageHandler handler = new(yandex.Answer);
        using HttpClient httpClient = new(handler);
        using YandexCloudImageTranslationProvider provider = new(Credentials, httpClient);

        ImageTranslationResult result = await provider.ExecuteAsync(
            new ImageTranslationRequest(new ProviderImage(Image, "image/png"), new LanguageId("russian")));

        Assert.Equal(2, result.Lines.Length);
    }

    [Fact]
    public async Task ExecuteAsync_ImageYandexDoesNotRead_ThrowsProviderExceptionBeforeAnyRequest()
    {
        YandexCloud yandex = new();

        using StubHttpMessageHandler handler = new(yandex.Answer);
        using HttpClient httpClient = new(handler);
        using YandexCloudImageTranslationProvider provider = new(Credentials, httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new ImageTranslationRequest(new ProviderImage(Image, "image/webp"), new LanguageId("russian"))));

        Assert.Contains("image/webp", exception.Message, StringComparison.Ordinal);
        Assert.Empty(yandex.Requests);
    }

    [Fact]
    public async Task ExecuteAsync_NothingOnTheImage_ReturnsNoLinesAndAsksNothingOfTranslate()
    {
        YandexCloud yandex = new(ocr: """{"textAnnotation":{"blocks":[]},"page":"0"}""");

        using StubHttpMessageHandler handler = new(yandex.Answer);
        using HttpClient httpClient = new(handler);
        using YandexCloudImageTranslationProvider provider = new(Credentials, httpClient);

        ImageTranslationResult result = await provider.ExecuteAsync(
            new ImageTranslationRequest(new ProviderImage(Image, "image/png"), new LanguageId("russian")));

        Assert.Empty(result.Lines);
        Assert.Single(yandex.Requests);
        Assert.Equal("russian", result.SourceLanguageId.Value);
    }

    [Fact]
    public async Task ExecuteAsync_LinesWithoutText_AreLeftOut()
    {
        YandexCloud yandex = new(
            ocr: """{"textAnnotation":{"blocks":[{"lines":[{"text":"  "},{"text":"Hello","boundingBox":{"vertices":[{"x":"1","y":"2"},{"x":"4","y":"6"}]}}]}]}}""",
            translation: """{"translations":[{"text":"Привет","detectedLanguageCode":"en"}]}""");

        using StubHttpMessageHandler handler = new(yandex.Answer);
        using HttpClient httpClient = new(handler);
        using YandexCloudImageTranslationProvider provider = new(Credentials, httpClient);

        ImageTranslationResult result = await provider.ExecuteAsync(
            new ImageTranslationRequest(new ProviderImage(Image, "image/png"), new LanguageId("russian")));

        TranslatedLine line = Assert.Single(result.Lines);

        Assert.Equal("Привет", line.TranslatedText);
        Assert.Equal(new TextBounds(1, 2, 3, 4), line.Bounds);
    }

    [Fact]
    public async Task ExecuteAsync_OcrRefusesTheRequest_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("""{"code":7,"message":"Vision OCR is not enabled."}""", Encoding.UTF8, "application/json"),
        });
        using HttpClient httpClient = new(handler);
        using YandexCloudImageTranslationProvider provider = new(Credentials, httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new ImageTranslationRequest(new ProviderImage(Image, "image/png"), new LanguageId("russian"))));

        Assert.Contains("Vision OCR is not enabled", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_OcrAnswerThatIsNotJson_ThrowsProviderException()
    {
        YandexCloud yandex = new(ocr: "garbage");

        using StubHttpMessageHandler handler = new(yandex.Answer);
        using HttpClient httpClient = new(handler);
        using YandexCloudImageTranslationProvider provider = new(Credentials, httpClient);

        await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new ImageTranslationRequest(new ProviderImage(Image, "image/png"), new LanguageId("russian"))));
    }
}
