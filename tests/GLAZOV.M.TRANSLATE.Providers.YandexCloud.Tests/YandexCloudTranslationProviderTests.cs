using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Abstractions.Translation;
using GLAZOV.M.TRANSLATE.Providers.YandexCloud.Internal;
using System.Net;
using System.Text;
using System.Text.Json;

namespace GLAZOV.M.TRANSLATE.Providers.YandexCloud.Tests;

/// <summary>
/// Verifies the text translation provider and the engine under it: what goes
/// to Yandex Cloud Translate and what is made of the answer.
/// </summary>
public sealed class YandexCloudTranslationProviderTests
{
    private static readonly YandexCloudCredentials Credentials = new("AQVN-secret-key");

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
    public void Name_IsYandexCloud()
    {
        using YandexCloudTranslationProvider provider = new(Credentials, new HttpClient());

        Assert.Equal("Yandex Cloud", provider.Name);
    }

    [Fact]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new YandexCloudTranslationProvider(null!));
        Assert.Throws<ArgumentNullException>(() => new YandexCloudTranslationProvider(null!, new HttpClient()));
        Assert.Throws<ArgumentNullException>(() => new YandexCloudTranslationProvider(Credentials, null!));
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        using YandexCloudTranslationProvider provider = new(Credentials, new HttpClient());

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsync_ExplicitSource_ReturnsTranslationWithoutDetectionFlag()
    {
        using StubHttpMessageHandler handler = Answering("""{"translations":[{"text":"Bonjour"}]}""");
        using HttpClient httpClient = new(handler);
        using YandexCloudTranslationProvider provider = new(Credentials, httpClient);

        TextTranslationRequest request = new(new ProviderText("Hello"), new LanguageId("french"), new LanguageId("english"));
        TextTranslationResult result = await provider.ExecuteAsync(request);

        Assert.Equal("Bonjour", result.TranslatedText.Value);
        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.Equal("french", result.TargetLanguageId.Value);
        Assert.False(result.WasSourceLanguageDetected);
        Assert.Equal(request.Id, result.RequestId);
    }

    [Fact]
    public async Task ExecuteAsync_NoSource_ReportsWhatYandexDetected()
    {
        string? body = null;

        using StubHttpMessageHandler handler = Answering(
            """{"translations":[{"text":"Привет","detectedLanguageCode":"en"}]}""",
            observe: (_, sent) => body = sent);
        using HttpClient httpClient = new(handler);
        using YandexCloudTranslationProvider provider = new(Credentials, httpClient);

        TextTranslationResult result = await provider.ExecuteAsync(
            new TextTranslationRequest(new ProviderText("Hello"), new LanguageId("russian")));

        using JsonDocument json = JsonDocument.Parse(body!);

        Assert.False(json.RootElement.TryGetProperty("sourceLanguageCode", out _));
        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.True(result.WasSourceLanguageDetected);
    }

    [Fact]
    public async Task ExecuteAsync_SendsPlainTextWithTheKeyInAHeaderNotTheAddress()
    {
        HttpRequestMessage? sent = null;
        string? body = null;

        using StubHttpMessageHandler handler = Answering(
            """{"translations":[{"text":"Bonjour"}]}""",
            observe: (request, content) =>
            {
                sent = request;
                body = content;
            });
        using HttpClient httpClient = new(handler);
        using YandexCloudTranslationProvider provider = new(Credentials, httpClient);

        await provider.ExecuteAsync(new TextTranslationRequest(
            new ProviderText("Hello"),
            new LanguageId("french"),
            new LanguageId("english")));

        Assert.Equal("https://translate.api.cloud.yandex.net/translate/v2/translate", sent!.RequestUri!.AbsoluteUri);
        Assert.Equal("Api-Key AQVN-secret-key", sent.Headers.Authorization!.ToString());
        Assert.DoesNotContain("AQVN-secret-key", sent.RequestUri.AbsoluteUri, StringComparison.Ordinal);

        using JsonDocument json = JsonDocument.Parse(body!);

        Assert.Equal("Hello", Assert.Single(json.RootElement.GetProperty("texts").EnumerateArray()).GetString());
        Assert.Equal("en", json.RootElement.GetProperty("sourceLanguageCode").GetString());
        Assert.Equal("fr", json.RootElement.GetProperty("targetLanguageCode").GetString());
        Assert.Equal("PLAIN_TEXT", json.RootElement.GetProperty("format").GetString());
        Assert.False(json.RootElement.TryGetProperty("folderId", out _));
    }

    [Fact]
    public async Task ExecuteAsync_CredentialsWithFolder_SendItInTheBody()
    {
        string? body = null;

        using StubHttpMessageHandler handler = Answering(
            """{"translations":[{"text":"Bonjour"}]}""",
            observe: (_, sent) => body = sent);
        using HttpClient httpClient = new(handler);
        using YandexCloudTranslationProvider provider = new(new YandexCloudCredentials("key", "b1g123"), httpClient);

        await provider.ExecuteAsync(new TextTranslationRequest(new ProviderText("Hello"), new LanguageId("french"), new LanguageId("english")));

        using JsonDocument json = JsonDocument.Parse(body!);

        Assert.Equal("b1g123", json.RootElement.GetProperty("folderId").GetString());
    }

    [Fact]
    public async Task ExecuteAsync_RefusedByYandex_ThrowsProviderExceptionWithItsExplanation()
    {
        using StubHttpMessageHandler handler = Answering(
            """{"code":16,"message":"API key is not valid or the folder has no access to the service."}""",
            HttpStatusCode.Unauthorized);
        using HttpClient httpClient = new(handler);
        using YandexCloudTranslationProvider provider = new(Credentials, httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new TextTranslationRequest(new ProviderText("Hello"), new LanguageId("french"))));

        Assert.Equal("Yandex Cloud", exception.ProviderName);
        Assert.Contains("401", exception.Message, StringComparison.Ordinal);
        Assert.Contains("API key is not valid", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("AQVN-secret-key", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_RefusedWithABodyThatIsNotAnError_StillThrowsProviderExceptionWithTheStatus()
    {
        using StubHttpMessageHandler handler = Answering("<html>Bad gateway</html>", HttpStatusCode.BadGateway);
        using HttpClient httpClient = new(handler);
        using YandexCloudTranslationProvider provider = new(Credentials, httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new TextTranslationRequest(new ProviderText("Hello"), new LanguageId("french"))));

        Assert.Contains("502", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_AnswerWithoutTranslation_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering("""{"translations":[]}""");
        using HttpClient httpClient = new(handler);
        using YandexCloudTranslationProvider provider = new(Credentials, httpClient);

        await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new TextTranslationRequest(new ProviderText("Hello"), new LanguageId("french"))));
    }

    [Fact]
    public async Task ExecuteAsync_AnswerThatIsNotJson_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering("not json");
        using HttpClient httpClient = new(handler);
        using YandexCloudTranslationProvider provider = new(Credentials, httpClient);

        await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new TextTranslationRequest(new ProviderText("Hello"), new LanguageId("french"))));
    }

    [Fact]
    public async Task Engine_PiecesOverTheLimitInAll_AreSentInSeveralRequestsAndComeBackInOrder()
    {
        List<int> sizes = [];

        using StubHttpMessageHandler handler = new(request =>
        {
            using JsonDocument json = JsonDocument.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
            string[] pieces = [.. json.RootElement.GetProperty("texts").EnumerateArray().Select(piece => piece.GetString()!)];

            sizes.Add(pieces.Length);

            string translations = string.Join(',', pieces.Select(piece => "{\"text\":\"" + piece[..5] + "!\"}"));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"translations\":[" + translations + "]}", Encoding.UTF8, "application/json"),
            };
        });
        using HttpClient httpClient = new(handler);
        using YandexCloudEngine engine = new(Credentials, httpClient);

        // Three pieces of 4,000 characters: two fit in a request of 10,000, the third does not.
        string[] pieces = [.. Enumerable.Range(0, 3).Select(number => "p" + number + new string('x', 3999))];

        (IReadOnlyList<string> translations, _) = await engine.TranslateAsync(pieces, isMarkup: false, "en", "fr");

        Assert.Equal([2, 1], sizes);
        Assert.Equal(["p0xxx!", "p1xxx!", "p2xxx!"], translations);
    }

    [Fact]
    public async Task Engine_PieceLongerThanARequestMayBe_ThrowsProviderExceptionBeforeAnyRequest()
    {
        bool sent = false;

        using StubHttpMessageHandler handler = Answering("{}", observe: (_, _) => sent = true);
        using HttpClient httpClient = new(handler);
        using YandexCloudEngine engine = new(Credentials, httpClient);

        await Assert.ThrowsAsync<ProviderException>(
            () => engine.TranslateAsync([new string('x', 10001)], isMarkup: false, "en", "fr"));

        Assert.False(sent);
    }

    [Fact]
    public async Task Engine_AnswerWithFewerTranslationsThanPieces_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering("""{"translations":[{"text":"one"}]}""");
        using HttpClient httpClient = new(handler);
        using YandexCloudEngine engine = new(Credentials, httpClient);

        await Assert.ThrowsAsync<ProviderException>(() => engine.TranslateAsync(["a", "b"], isMarkup: false, "en", "fr"));
    }
}
