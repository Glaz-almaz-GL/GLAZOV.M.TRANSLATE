using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Abstractions.Translation;
using System.Net;
using System.Text;
using System.Text.Json;

namespace GLAZOV.M.TRANSLATE.Providers.GoogleCloud.Tests;

/// <summary>
/// Verifies the markup translation provider: that it asks for HTML, and gives
/// back the markup it answers with.
/// </summary>
public sealed class GoogleCloudMarkupTranslationProviderTests
{
    private static readonly GoogleCloudCredentials Credentials = new("AIza-secret-key");

    [Fact]
    public void Name_IsGoogleCloud()
    {
        using GoogleCloudMarkupTranslationProvider provider = new(Credentials, new HttpClient());

        Assert.Equal("Google Cloud", provider.Name);
    }

    [Fact]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new GoogleCloudMarkupTranslationProvider(null!));
        Assert.Throws<ArgumentNullException>(() => new GoogleCloudMarkupTranslationProvider(Credentials, null!));
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        using GoogleCloudMarkupTranslationProvider provider = new(Credentials, new HttpClient());

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
                    """{"data":{"translations":[{"translatedText":"<b>Привет</b> мир","detectedSourceLanguage":"en"}]}}""",
                    Encoding.UTF8,
                    "application/json"),
            };
        });
        using HttpClient httpClient = new(handler);
        using GoogleCloudMarkupTranslationProvider provider = new(Credentials, httpClient);

        MarkupTranslationResult result = await provider.ExecuteAsync(
            new MarkupTranslationRequest(new ProviderMarkup("<b>Hello</b> world"), new LanguageId("russian")));

        using JsonDocument json = JsonDocument.Parse(body!);

        Assert.Equal("html", json.RootElement.GetProperty("format").GetString());
        Assert.Equal("<b>Hello</b> world", Assert.Single(json.RootElement.GetProperty("q").EnumerateArray()).GetString());

        Assert.Equal("<b>Привет</b> мир", result.TranslatedMarkup.Value);
        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.True(result.WasSourceLanguageDetected);
    }

    [Fact]
    public async Task ExecuteAsync_ExplicitSource_IsKept()
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"data":{"translations":[{"translatedText":"<i>Bonjour</i>"}]}}""",
                Encoding.UTF8,
                "application/json"),
        });
        using HttpClient httpClient = new(handler);
        using GoogleCloudMarkupTranslationProvider provider = new(Credentials, httpClient);

        MarkupTranslationResult result = await provider.ExecuteAsync(new MarkupTranslationRequest(
            new ProviderMarkup("<i>Hello</i>"),
            new LanguageId("french"),
            new LanguageId("english")));

        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.False(result.WasSourceLanguageDetected);
    }

    [Fact]
    public async Task ExecuteAsync_RefusedByGoogle_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"error":{"code":400,"message":"API key not valid."}}""", Encoding.UTF8, "application/json"),
        });
        using HttpClient httpClient = new(handler);
        using GoogleCloudMarkupTranslationProvider provider = new(Credentials, httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new MarkupTranslationRequest(new ProviderMarkup("<b>Hello</b>"), new LanguageId("french"))));

        Assert.Contains("API key not valid", exception.Message, StringComparison.Ordinal);
    }
}
