using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;
using System.Net;
using System.Text;

namespace GLTranslate.Providers.Yandex.Tests;

/// <summary>
/// Verifies the markup translation provider: the request it builds, the
/// answer it reads, and what it refuses.
/// </summary>
public sealed class YandexMarkupTranslationProviderTests
{
    private const string Markup = "<p>Good <b>morning</b></p>";

    private const string Answer = """{"code":200,"lang":"en-ru","text":["<p>Доброе <b>утро</b></p>"]}""";

    private static StubHttpMessageHandler Answering(
        string json,
        HttpStatusCode status = HttpStatusCode.OK,
        Action<HttpRequestMessage>? observe = null)
    {
        return new StubHttpMessageHandler(request =>
        {
            observe?.Invoke(request);

            return new HttpResponseMessage(status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        });
    }

    [Fact]
    public void Name_IsYandex()
    {
        using YandexMarkupTranslationProvider provider = new(new HttpClient());

        Assert.Equal("Yandex", provider.Name);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutSourceLanguage_ReportsTheDetectedOne()
    {
        using StubHttpMessageHandler handler = Answering(Answer);
        using HttpClient httpClient = new(handler);
        using YandexMarkupTranslationProvider provider = new(httpClient);

        MarkupTranslationRequest request = new(new ProviderMarkup(Markup), new LanguageId("russian"));
        MarkupTranslationResult result = await provider.ExecuteAsync(request);

        Assert.Equal("<p>Доброе <b>утро</b></p>", result.TranslatedMarkup.Value);
        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.Equal("russian", result.TargetLanguageId.Value);
        Assert.True(result.WasSourceLanguageDetected);
        Assert.Equal(request.Id, result.RequestId);
    }

    [Fact]
    public async Task ExecuteAsync_WithSourceLanguage_NamesTheDirection()
    {
        string? body = null;

        using StubHttpMessageHandler handler = Answering(
            Answer,
            observe: request => body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());

        using HttpClient httpClient = new(handler);
        using YandexMarkupTranslationProvider provider = new(httpClient);

        MarkupTranslationRequest request = new(
            new ProviderMarkup(Markup),
            new LanguageId("russian"),
            new LanguageId("english"));

        MarkupTranslationResult result = await provider.ExecuteAsync(request);

        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.False(result.WasSourceLanguageDetected);
        Assert.Contains("lang=en-ru", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_AsksTheEndpointForHtml()
    {
        Uri? requestUri = null;

        using StubHttpMessageHandler handler = Answering(Answer, observe: request => requestUri = request.RequestUri);
        using HttpClient httpClient = new(handler);
        using YandexMarkupTranslationProvider provider = new(httpClient);

        await provider.ExecuteAsync(new MarkupTranslationRequest(new ProviderMarkup(Markup), new LanguageId("russian")));

        Assert.EndsWith("/translate", requestUri!.AbsolutePath, StringComparison.Ordinal);
        Assert.Contains("format=html", requestUri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_RefusalInsideTheAnswer_ThrowsProviderExceptionWithItsMessage()
    {
        using StubHttpMessageHandler handler = Answering(
            """{"code":501,"message":"The specified translation direction is not supported"}""",
            HttpStatusCode.BadRequest);

        using HttpClient httpClient = new(handler);
        using YandexMarkupTranslationProvider provider = new(httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new MarkupTranslationRequest(new ProviderMarkup(Markup), new LanguageId("russian"))));

        Assert.Equal("Yandex", exception.ProviderName);
        Assert.Contains("not supported", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_NoTranslation_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering("""{"code":200,"lang":"en-ru","text":[]}""");
        using HttpClient httpClient = new(handler);
        using YandexMarkupTranslationProvider provider = new(httpClient);

        await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new MarkupTranslationRequest(new ProviderMarkup(Markup), new LanguageId("russian"))));
    }

    [Fact]
    public async Task ExecuteAsync_NoDirection_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering("""{"code":200,"text":["<p>Доброе <b>утро</b></p>"]}""");
        using HttpClient httpClient = new(handler);
        using YandexMarkupTranslationProvider provider = new(httpClient);

        await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new MarkupTranslationRequest(new ProviderMarkup(Markup), new LanguageId("russian"))));
    }

    [Fact]
    public async Task ExecuteAsync_UnreadableAnswer_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering("not json at all");
        using HttpClient httpClient = new(handler);
        using YandexMarkupTranslationProvider provider = new(httpClient);

        await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new MarkupTranslationRequest(new ProviderMarkup(Markup), new LanguageId("russian"))));
    }

    [Fact]
    public async Task ExecuteAsync_UnknownLanguage_ThrowsProviderException()
    {
        using YandexMarkupTranslationProvider provider = new(new HttpClient());

        await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new MarkupTranslationRequest(new ProviderMarkup(Markup), new LanguageId("does_not_exist"))));
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        using YandexMarkupTranslationProvider provider = new(new HttpClient());

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.ExecuteAsync(null!));
    }

    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new YandexMarkupTranslationProvider(null!));
    }

    [Fact]
    public void Dispose_Twice_DoesNotThrow()
    {
        YandexMarkupTranslationProvider provider = new(new HttpClient());

        provider.Dispose();

        Assert.Null(Record.Exception(provider.Dispose));
    }

    [LiveFact]
    public async Task ExecuteAsync_LiveYandex_KeepsTheTags()
    {
        using YandexMarkupTranslationProvider provider = new();

        MarkupTranslationResult result = await provider.ExecuteAsync(
            new MarkupTranslationRequest(new ProviderMarkup("<p>Good <b>morning</b>, world!</p>"), new LanguageId("russian")));

        Assert.StartsWith("<p>", result.TranslatedMarkup.Value, StringComparison.Ordinal);
        Assert.Contains("<b>", result.TranslatedMarkup.Value, StringComparison.Ordinal);
        Assert.EndsWith("</p>", result.TranslatedMarkup.Value, StringComparison.Ordinal);
        Assert.Equal("english", result.SourceLanguageId.Value);
    }
}
