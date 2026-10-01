using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;
using GLTranslate.Providers.GoogleCloud.Internal;
using System.Net;
using System.Text;
using System.Text.Json;

namespace GLTranslate.Providers.GoogleCloud.Tests;

/// <summary>
/// Verifies the text translation provider and the engine under it: what goes
/// to Google Cloud Translation and what is made of the answer.
/// </summary>
public sealed class GoogleCloudTranslationProviderTests
{
    private static readonly GoogleCloudCredentials Credentials = new("AIza-secret-key");

    private static StubHttpMessageHandler Answering(
        string json,
        HttpStatusCode status = HttpStatusCode.OK,
        Action<HttpRequestMessage, string>? observe = null)
    {
        return new StubHttpMessageHandler(request =>
        {
            observe?.Invoke(request, request.Content is null ? string.Empty : request.Content.ReadAsStringAsync().GetAwaiter().GetResult());

            return new HttpResponseMessage(status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        });
    }

    [Fact]
    public void Name_IsGoogleCloud()
    {
        using GoogleCloudTranslationProvider provider = new(Credentials, new HttpClient());

        Assert.Equal("Google Cloud", provider.Name);
    }

    [Fact]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new GoogleCloudTranslationProvider(null!));
        Assert.Throws<ArgumentNullException>(() => new GoogleCloudTranslationProvider(null!, new HttpClient()));
        Assert.Throws<ArgumentNullException>(() => new GoogleCloudTranslationProvider(Credentials, null!));
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        using GoogleCloudTranslationProvider provider = new(Credentials, new HttpClient());

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsync_ExplicitSource_ReturnsTranslationWithoutDetectionFlag()
    {
        using StubHttpMessageHandler handler = Answering("""{"data":{"translations":[{"translatedText":"Bonjour"}]}}""");
        using HttpClient httpClient = new(handler);
        using GoogleCloudTranslationProvider provider = new(Credentials, httpClient);

        TextTranslationRequest request = new(new ProviderText("Hello"), new LanguageId("french"), new LanguageId("english"));
        TextTranslationResult result = await provider.ExecuteAsync(request);

        Assert.Equal("Bonjour", result.TranslatedText.Value);
        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.Equal("french", result.TargetLanguageId.Value);
        Assert.False(result.WasSourceLanguageDetected);
        Assert.Equal(request.Id, result.RequestId);
    }

    [Fact]
    public async Task ExecuteAsync_NoSource_ReportsWhatGoogleDetected()
    {
        string? body = null;

        using StubHttpMessageHandler handler = Answering(
            """{"data":{"translations":[{"translatedText":"Привет","detectedSourceLanguage":"en"}]}}""",
            observe: (_, sent) => body = sent);
        using HttpClient httpClient = new(handler);
        using GoogleCloudTranslationProvider provider = new(Credentials, httpClient);

        TextTranslationResult result = await provider.ExecuteAsync(
            new TextTranslationRequest(new ProviderText("Hello"), new LanguageId("russian")));

        using JsonDocument json = JsonDocument.Parse(body!);

        Assert.False(json.RootElement.TryGetProperty("source", out _));
        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.True(result.WasSourceLanguageDetected);
    }

    [Fact]
    public async Task ExecuteAsync_SendsPlainTextWithTheKeyInAHeaderNotTheAddress()
    {
        HttpRequestMessage? sent = null;
        string? body = null;

        using StubHttpMessageHandler handler = Answering(
            """{"data":{"translations":[{"translatedText":"Bonjour"}]}}""",
            observe: (request, content) =>
            {
                sent = request;
                body = content;
            });
        using HttpClient httpClient = new(handler);
        using GoogleCloudTranslationProvider provider = new(Credentials, httpClient);

        await provider.ExecuteAsync(new TextTranslationRequest(
            new ProviderText("Hello"),
            new LanguageId("french"),
            new LanguageId("english")));

        Assert.Equal("https://translation.googleapis.com/language/translate/v2", sent!.RequestUri!.AbsoluteUri);
        Assert.Equal("AIza-secret-key", Assert.Single(sent.Headers.GetValues("X-Goog-Api-Key")));
        Assert.DoesNotContain("AIza-secret-key", sent.RequestUri.AbsoluteUri, StringComparison.Ordinal);

        using JsonDocument json = JsonDocument.Parse(body!);

        Assert.Equal("Hello", Assert.Single(json.RootElement.GetProperty("q").EnumerateArray()).GetString());
        Assert.Equal("en", json.RootElement.GetProperty("source").GetString());
        Assert.Equal("fr", json.RootElement.GetProperty("target").GetString());

        // Google takes plain text for HTML unless told, and escapes it.
        Assert.Equal("text", json.RootElement.GetProperty("format").GetString());
    }

    [Fact]
    public async Task ExecuteAsync_RefusedByGoogle_ThrowsProviderExceptionWithItsExplanation()
    {
        using StubHttpMessageHandler handler = Answering(
            """{"error":{"code":403,"message":"Cloud Translation API has not been used in project 1 before or it is disabled."}}""",
            HttpStatusCode.Forbidden);
        using HttpClient httpClient = new(handler);
        using GoogleCloudTranslationProvider provider = new(Credentials, httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new TextTranslationRequest(new ProviderText("Hello"), new LanguageId("french"))));

        Assert.Equal("Google Cloud", exception.ProviderName);
        Assert.Contains("403", exception.Message, StringComparison.Ordinal);
        Assert.Contains("disabled", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("AIza-secret-key", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_RefusedWithABodyThatIsNotAnError_StillThrowsProviderExceptionWithTheStatus()
    {
        using StubHttpMessageHandler handler = Answering("<html>Bad gateway</html>", HttpStatusCode.BadGateway);
        using HttpClient httpClient = new(handler);
        using GoogleCloudTranslationProvider provider = new(Credentials, httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new TextTranslationRequest(new ProviderText("Hello"), new LanguageId("french"))));

        Assert.Contains("502", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_AnswerWithoutTranslation_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering("""{"data":{"translations":[]}}""");
        using HttpClient httpClient = new(handler);
        using GoogleCloudTranslationProvider provider = new(Credentials, httpClient);

        await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new TextTranslationRequest(new ProviderText("Hello"), new LanguageId("french"))));
    }

    [Fact]
    public async Task ExecuteAsync_AnswerThatIsNotJson_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering("not json");
        using HttpClient httpClient = new(handler);
        using GoogleCloudTranslationProvider provider = new(Credentials, httpClient);

        await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new TextTranslationRequest(new ProviderText("Hello"), new LanguageId("french"))));
    }

    [Fact]
    public async Task Engine_MoreThan128Pieces_AreSentInSeveralRequestsAndComeBackInOrder()
    {
        List<int> sizes = [];

        using StubHttpMessageHandler handler = new(request =>
        {
            using JsonDocument json = JsonDocument.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
            string[] pieces = [.. json.RootElement.GetProperty("q").EnumerateArray().Select(piece => piece.GetString()!)];

            sizes.Add(pieces.Length);

            string translations = string.Join(',', pieces.Select(piece => "{\"translatedText\":\"" + piece + "!\"}"));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":{\"translations\":[" + translations + "]}}", Encoding.UTF8, "application/json"),
            };
        });
        using HttpClient httpClient = new(handler);
        using GoogleCloudEngine engine = new(Credentials, httpClient);

        string[] pieces = [.. Enumerable.Range(0, 130).Select(number => $"line{number}")];

        (IReadOnlyList<string> translations, _) = await engine.TranslateAsync(pieces, isMarkup: false, "en", "fr");

        Assert.Equal([128, 2], sizes);
        Assert.Equal(130, translations.Count);
        Assert.Equal("line0!", translations[0]);
        Assert.Equal("line129!", translations[129]);
    }

    [Fact]
    public async Task Engine_AnswerWithFewerTranslationsThanPieces_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering("""{"data":{"translations":[{"translatedText":"one"}]}}""");
        using HttpClient httpClient = new(handler);
        using GoogleCloudEngine engine = new(Credentials, httpClient);

        await Assert.ThrowsAsync<ProviderException>(() => engine.TranslateAsync(["a", "b"], isMarkup: false, "en", "fr"));
    }
}
