using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Abstractions.Translation;
using System.Net;
using System.Text;

namespace GLAZOV.M.TRANSLATE.Providers.Yandex.Tests;

/// <summary>
/// Verifies the image translation provider: what it reads from the
/// recognition endpoint, what it sends to the translation one, and how the
/// two are put together.
/// </summary>
public sealed class YandexImageTranslationProviderTests
{
    private const string Recognized = """
        {"status":"success","data":{"detected_lang":"en","blocks":[{"boxes":[
        {"x":10,"y":59,"w":374,"h":20,"text":"Good morning","words":[
        {"x":10,"y":55,"w":87,"h":28,"text":"Good"},
        {"x":113,"y":55,"w":152,"h":28,"text":"morning"}]},
        {"x":10,"y":90,"w":300,"h":20,"text":"Good evening","words":[]}]}]}}
        """;

    private const string Translated = """{"code":200,"lang":"en-ru","text":["Доброе утро","Добрый вечер"]}""";

    private static readonly byte[] Image = [0xFF, 0xD8, 0xFF, 0xE0];

    /// <summary>
    /// Answers the recognition endpoint with what was read and the translation
    /// endpoint with the translations, handing each request to the observer.
    /// </summary>
    private static StubHttpMessageHandler Answering(
        string recognized = Recognized,
        string translated = Translated,
        Action<HttpRequestMessage>? observe = null)
    {
        return new StubHttpMessageHandler(request =>
        {
            observe?.Invoke(request);

            bool isRecognition = request.RequestUri!.AbsolutePath.Contains("recognize", StringComparison.Ordinal);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    isRecognition ? recognized : translated,
                    Encoding.UTF8,
                    "application/json"),
            };
        });
    }

    [Fact]
    public void Name_IsYandex()
    {
        using YandexImageTranslationProvider provider = new(new HttpClient());

        Assert.Equal("Yandex", provider.Name);
    }

    [Fact]
    public async Task ExecuteAsync_Answer_ReturnsEveryLineWithItsPlace()
    {
        using StubHttpMessageHandler handler = Answering();
        using HttpClient httpClient = new(handler);
        using YandexImageTranslationProvider provider = new(httpClient);

        ImageTranslationRequest request = new(new ProviderImage(Image), new LanguageId("russian"));
        ImageTranslationResult result = await provider.ExecuteAsync(request);

        Assert.Equal(2, result.Lines.Length);

        TranslatedLine first = result.Lines[0];

        Assert.Equal("Good morning", first.RecognizedText);
        Assert.Equal("Доброе утро", first.TranslatedText);
        Assert.Equal(new TextBounds(10, 59, 374, 20), first.Bounds);
        Assert.Equal(2, first.Words.Length);
        Assert.Equal("Good", first.Words[0].Text);
        Assert.Equal(new TextBounds(10, 55, 87, 28), first.Words[0].Bounds);

        Assert.Equal("Добрый вечер", result.Lines[1].TranslatedText);
        Assert.Empty(result.Lines[1].Words);

        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.Equal("russian", result.TargetLanguageId.Value);
        Assert.True(result.WasSourceLanguageDetected);
        Assert.Equal(request.Id, result.RequestId);
    }

    [Fact]
    public async Task ExecuteAsync_TranslatesEveryLineInOneRequest()
    {
        List<string> bodies = [];

        using StubHttpMessageHandler handler = Answering(observe: request =>
        {
            if (!request.RequestUri!.AbsolutePath.Contains("recognize", StringComparison.Ordinal))
            {
                bodies.Add(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
            }
        });

        using HttpClient httpClient = new(handler);
        using YandexImageTranslationProvider provider = new(httpClient);

        await provider.ExecuteAsync(new ImageTranslationRequest(new ProviderImage(Image), new LanguageId("russian")));

        string body = Assert.Single(bodies);

        Assert.Contains("text=Good+morning", body, StringComparison.Ordinal);
        Assert.Contains("text=Good+evening", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_WithSourceLanguage_KeepsItAndTellsTheReader()
    {
        List<Uri> uris = [];

        using StubHttpMessageHandler handler = Answering(observe: request => uris.Add(request.RequestUri!));
        using HttpClient httpClient = new(handler);
        using YandexImageTranslationProvider provider = new(httpClient);

        ImageTranslationResult result = await provider.ExecuteAsync(
            new ImageTranslationRequest(new ProviderImage(Image), new LanguageId("russian"), new LanguageId("english")));

        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.False(result.WasSourceLanguageDetected);
        Assert.Contains("lang=en", uris[0].Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_ImageWithoutText_ReturnsNoLines()
    {
        using StubHttpMessageHandler handler = Answering(recognized: """{"status":"success","data":{"detected_lang":"en","blocks":[]}}""");
        using HttpClient httpClient = new(handler);
        using YandexImageTranslationProvider provider = new(httpClient);

        ImageTranslationResult result = await provider.ExecuteAsync(
            new ImageTranslationRequest(new ProviderImage(Image), new LanguageId("russian")));

        Assert.Empty(result.Lines);
        Assert.Equal("english", result.SourceLanguageId.Value);
    }

    [Fact]
    public async Task ExecuteAsync_UnreadableImage_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering(recognized: """{"error":"UnsupportedMediaType"}""");
        using HttpClient httpClient = new(handler);
        using YandexImageTranslationProvider provider = new(httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new ImageTranslationRequest(new ProviderImage(Image), new LanguageId("russian"))));

        Assert.Equal("Yandex", exception.ProviderName);
        Assert.Contains("could not read the image", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownLanguage_ThrowsProviderException()
    {
        using YandexImageTranslationProvider provider = new(new HttpClient());

        await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new ImageTranslationRequest(new ProviderImage(Image), new LanguageId("does_not_exist"))));
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        using YandexImageTranslationProvider provider = new(new HttpClient());

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.ExecuteAsync(null!));
    }

    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new YandexImageTranslationProvider(null!));
    }

    [Fact]
    public void ProviderImage_EmptyContent_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new ProviderImage([]));
    }

    [Fact]
    public void Dispose_Twice_DoesNotThrow()
    {
        YandexImageTranslationProvider provider = new(new HttpClient());

        provider.Dispose();

        Assert.Null(Record.Exception(provider.Dispose));
    }

    [LiveFact]
    public async Task ExecuteAsync_LiveYandex_ReadsTheSampleImageAndTranslatesIt()
    {
        // SampleImage.png carries the single line "Good morning, world!".
        byte[] image = await File.ReadAllBytesAsync(
            Path.Combine(AppContext.BaseDirectory, "SampleImage.png"));

        using YandexImageTranslationProvider provider = new();

        ImageTranslationResult result = await provider.ExecuteAsync(
            new ImageTranslationRequest(new ProviderImage(image, "image/png", "sample.png"), new LanguageId("russian")));

        TranslatedLine line = Assert.Single(result.Lines);

        Assert.Contains("morning", line.RecognizedText, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(line.TranslatedText));
        Assert.NotEqual(0, line.Bounds.Width);
        Assert.NotEmpty(line.Words);
        Assert.Equal("english", result.SourceLanguageId.Value);
    }
}
