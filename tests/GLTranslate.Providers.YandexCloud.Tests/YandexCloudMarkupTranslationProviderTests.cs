using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;
using System.Net;
using System.Text;
using System.Text.Json;

namespace GLTranslate.Providers.YandexCloud.Tests;

/// <summary>
/// Verifies the markup translation provider: that it asks for HTML, and gives
/// back the markup it answers with.
/// </summary>
public sealed class YandexCloudMarkupTranslationProviderTests
{
    private static readonly YandexCloudCredentials Credentials = new("AQVN-secret-key");

    [Fact]
    public void Name_IsYandexCloud()
    {
        using YandexCloudMarkupTranslationProvider provider = new(Credentials, new HttpClient());

        Assert.Equal("Yandex Cloud", provider.Name);
    }

    [Fact]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new YandexCloudMarkupTranslationProvider(null!));
        Assert.Throws<ArgumentNullException>(() => new YandexCloudMarkupTranslationProvider(Credentials, null!));
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        using YandexCloudMarkupTranslationProvider provider = new(Credentials, new HttpClient());

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsync_AsksForHtmlAndReturnsTheTranslatedMarkup()
    {
        string? body = null;

        using StubHttpMessageHandler handler = new(request =>
        {
            body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"translations":[{"text":"<b>Привет</b> мир","detectedLanguageCode":"en"}]}""",
                    Encoding.UTF8,
                    "application/json"),
            };
        });
        using HttpClient httpClient = new(handler);
        using YandexCloudMarkupTranslationProvider provider = new(Credentials, httpClient);

        MarkupTranslationResult result = await provider.ExecuteAsync(
            new MarkupTranslationRequest(new ProviderMarkup("<b>Hello</b> world"), new LanguageId("russian")));

        using JsonDocument json = JsonDocument.Parse(body!);

        Assert.Equal("HTML", json.RootElement.GetProperty("format").GetString());
        Assert.Equal("<b>Hello</b> world", Assert.Single(json.RootElement.GetProperty("texts").EnumerateArray()).GetString());

        Assert.Equal("<b>Привет</b> мир", result.TranslatedMarkup.Value);
        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.True(result.WasSourceLanguageDetected);
    }

    [Fact]
    public async Task ExecuteAsync_ExplicitSource_IsKept()
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"translations":[{"text":"<i>Bonjour</i>"}]}""", Encoding.UTF8, "application/json"),
        });
        using HttpClient httpClient = new(handler);
        using YandexCloudMarkupTranslationProvider provider = new(Credentials, httpClient);

        MarkupTranslationResult result = await provider.ExecuteAsync(new MarkupTranslationRequest(
            new ProviderMarkup("<i>Hello</i>"),
            new LanguageId("french"),
            new LanguageId("english")));

        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.False(result.WasSourceLanguageDetected);
    }

    [Fact]
    public async Task ExecuteAsync_RefusedByYandex_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"code":3,"message":"invalid html"}""", Encoding.UTF8, "application/json"),
        });
        using HttpClient httpClient = new(handler);
        using YandexCloudMarkupTranslationProvider provider = new(Credentials, httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new MarkupTranslationRequest(new ProviderMarkup("<b>Hello"), new LanguageId("french"))));

        Assert.Contains("invalid html", exception.Message, StringComparison.Ordinal);
    }
}
