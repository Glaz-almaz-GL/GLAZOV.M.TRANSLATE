using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Abstractions.Transliteration;
using System.Net;
using System.Text;

namespace GLAZOV.M.TRANSLATE.Providers.Bing.Tests;

/// <summary>
/// Verifies the transliteration provider, which reads the rendering Bing
/// produces while translating.
/// </summary>
public sealed class BingTransliterationProviderTests
{
    private const string Page = """<html><script>var params_AbusePreventionHelper = [9999999999999,"the-token",3600000];</script></html>""";

    private const string Answer =
        """[{"translations":[{"text":"Good morning","to":"en"}],"detectedLanguage":{"language":"ru"}},{"inputTransliteration":"Dobroe utro","script":"Latn"}]""";

    private static StubHttpMessageHandler Answering(string answer, Action<HttpRequestMessage>? observe = null)
    {
        return new StubHttpMessageHandler(request =>
        {
            observe?.Invoke(request);

            bool isPage = request.RequestUri!.AbsolutePath.EndsWith("/translator", StringComparison.Ordinal);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = isPage
                    ? new StringContent(Page, Encoding.UTF8, "text/html")
                    : new StringContent(answer, Encoding.UTF8, "application/json"),
            };
        });
    }

    [Fact]
    public void Name_IsBing()
    {
        using BingTransliterationProvider provider = new(new HttpClient());

        Assert.Equal("Bing", provider.Name);
    }

    [Fact]
    public async Task ExecuteAsync_NamedLanguage_ReturnsTheRendering()
    {
        using StubHttpMessageHandler handler = Answering(Answer);
        using HttpClient httpClient = new(handler);
        using BingTransliterationProvider provider = new(httpClient);

        TransliterationRequest request = new(new ProviderText("Доброе утро"), new LanguageId("russian"));
        TransliterationResult result = await provider.ExecuteAsync(request);

        Assert.Equal("Dobroe utro", result.Transliteration.Value);
        Assert.Equal("russian", result.LanguageId.Value);
        Assert.False(result.WasLanguageDetected);
        Assert.Equal(request.Id, result.RequestId);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutLanguage_ReportsTheDetectedOne()
    {
        using StubHttpMessageHandler handler = Answering(Answer);
        using HttpClient httpClient = new(handler);
        using BingTransliterationProvider provider = new(httpClient);

        TransliterationResult result = await provider.ExecuteAsync(
            new TransliterationRequest(new ProviderText("Доброе утро")));

        Assert.Equal("Dobroe utro", result.Transliteration.Value);
        Assert.Equal("russian", result.LanguageId.Value);
        Assert.True(result.WasLanguageDetected);
    }

    [Fact]
    public async Task ExecuteAsync_NothingToRender_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering(
            """[{"translations":[{"text":"Good morning","to":"en"}],"detectedLanguage":{"language":"en"}}]""");

        using HttpClient httpClient = new(handler);
        using BingTransliterationProvider provider = new(httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new TransliterationRequest(new ProviderText("Good morning"), new LanguageId("english"))));

        Assert.Contains("already written in the Latin script", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownLanguage_ThrowsProviderException()
    {
        using BingTransliterationProvider provider = new(new HttpClient());

        await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new TransliterationRequest(new ProviderText("Доброе утро"), new LanguageId("does_not_exist"))));
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        using BingTransliterationProvider provider = new(new HttpClient());

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.ExecuteAsync(null!));
    }

    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new BingTransliterationProvider(null!));
    }

    [LiveFact]
    public async Task ExecuteAsync_LiveBing_RendersRussianInLatin()
    {
        using BingTransliterationProvider provider = new();

        TransliterationResult result = await provider.ExecuteAsync(
            new TransliterationRequest(new ProviderText("Доброе утро"), new LanguageId("russian")));

        Assert.False(string.IsNullOrWhiteSpace(result.Transliteration.Value));
        Assert.Equal("russian", result.LanguageId.Value);
    }
}
