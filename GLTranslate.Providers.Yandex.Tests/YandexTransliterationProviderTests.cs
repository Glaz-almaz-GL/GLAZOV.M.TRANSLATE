using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Transliteration;
using System.Net;
using System.Text;

namespace GLTranslate.Providers.Yandex.Tests;

/// <summary>
/// Verifies the transliteration provider, including the detection it falls
/// back on when the request names no language.
/// </summary>
public sealed class YandexTransliterationProviderTests
{
    private const string Transliteration = "\"dobroe utro\"";

    private const string Detection = """{"code":200,"lang":"ru"}""";

    /// <summary>
    /// Answers the detection endpoint with a language and the transliteration
    /// endpoint with a transliteration, handing each request to the observer.
    /// </summary>
    private static StubHttpMessageHandler Answering(Action<HttpRequestMessage>? observe = null)
    {
        return new StubHttpMessageHandler(request =>
        {
            observe?.Invoke(request);

            string json = request.RequestUri!.AbsolutePath.Contains("detect", StringComparison.Ordinal)
                ? Detection
                : Transliteration;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        });
    }

    [Fact]
    public void Name_IsYandex()
    {
        using YandexTransliterationProvider provider = new(new HttpClient());

        Assert.Equal("Yandex", provider.Name);
    }

    [Fact]
    public async Task ExecuteAsync_NamedLanguage_ReturnsTransliteration()
    {
        List<Uri> uris = [];

        using StubHttpMessageHandler handler = Answering(request => uris.Add(request.RequestUri!));
        using HttpClient httpClient = new(handler);
        using YandexTransliterationProvider provider = new(httpClient);

        TransliterationRequest request = new(new ProviderText("Доброе утро"), new LanguageId("russian"));
        TransliterationResult result = await provider.ExecuteAsync(request);

        Assert.Equal("dobroe utro", result.Transliteration.Value);
        Assert.Equal("russian", result.LanguageId.Value);
        Assert.False(result.WasLanguageDetected);
        Assert.Single(uris);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutLanguage_DetectsItFirst()
    {
        List<Uri> uris = [];

        using StubHttpMessageHandler handler = Answering(request => uris.Add(request.RequestUri!));
        using HttpClient httpClient = new(handler);
        using YandexTransliterationProvider provider = new(httpClient);

        TransliterationResult result = await provider.ExecuteAsync(
            new TransliterationRequest(new ProviderText("Доброе утро")));

        Assert.Equal("dobroe utro", result.Transliteration.Value);
        Assert.Equal("russian", result.LanguageId.Value);
        Assert.True(result.WasLanguageDetected);
        Assert.Equal(2, uris.Count);
        Assert.Contains("detect", uris[0].AbsolutePath, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_NamesTheDirectionIntoLatin()
    {
        string? body = null;

        using StubHttpMessageHandler handler = Answering(
            request => body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());

        using HttpClient httpClient = new(handler);
        using YandexTransliterationProvider provider = new(httpClient);

        await provider.ExecuteAsync(new TransliterationRequest(new ProviderText("Доброе утро"), new LanguageId("russian")));

        Assert.Contains("lang=ru-en", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyTransliteration_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("\"\"", Encoding.UTF8, "application/json"),
        });

        using HttpClient httpClient = new(handler);
        using YandexTransliterationProvider provider = new(httpClient);

        await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new TransliterationRequest(new ProviderText("Доброе утро"), new LanguageId("russian"))));
    }

    [Fact]
    public async Task ExecuteAsync_FailedRequest_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("\"\"", Encoding.UTF8, "application/json"),
        });

        using HttpClient httpClient = new(handler);
        using YandexTransliterationProvider provider = new(httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new TransliterationRequest(new ProviderText("Доброе утро"), new LanguageId("russian"))));

        Assert.Equal("Yandex", exception.ProviderName);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownLanguage_ThrowsProviderException()
    {
        using YandexTransliterationProvider provider = new(new HttpClient());

        await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new TransliterationRequest(new ProviderText("Доброе утро"), new LanguageId("does_not_exist"))));
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        using YandexTransliterationProvider provider = new(new HttpClient());

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.ExecuteAsync(null!));
    }

    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new YandexTransliterationProvider(null!));
    }

    [LiveFact]
    public async Task ExecuteAsync_LiveYandex_TransliteratesText()
    {
        using YandexTransliterationProvider provider = new();

        TransliterationRequest request = new(new ProviderText("Доброе утро"), new LanguageId("russian"));
        TransliterationResult result = await provider.ExecuteAsync(request);

        Assert.False(string.IsNullOrWhiteSpace(result.Transliteration.Value));
        Assert.Equal("russian", result.LanguageId.Value);
    }

    [LiveFact]
    public async Task ExecuteAsync_LiveYandex_DetectsTheLanguageItself()
    {
        using YandexTransliterationProvider provider = new();

        TransliterationResult result = await provider.ExecuteAsync(
            new TransliterationRequest(new ProviderText("Доброе утро")));

        Assert.Equal("russian", result.LanguageId.Value);
        Assert.True(result.WasLanguageDetected);
    }
}
