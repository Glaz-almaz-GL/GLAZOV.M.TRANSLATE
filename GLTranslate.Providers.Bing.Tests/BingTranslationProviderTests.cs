using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;
using System.Net;
using System.Text;

namespace GLTranslate.Providers.Bing.Tests;

/// <summary>
/// Verifies the translation provider from the request it is given to the
/// result it returns, including the page it reads its credentials from.
/// </summary>
public sealed class BingTranslationProviderTests
{
    private const string Page = """<html><script>var params_AbusePreventionHelper = [9999999999999,"the-token",3600000];</script></html>""";

    private const string Answer =
        """[{"translations":[{"text":"Доброе утро","to":"ru"}],"detectedLanguage":{"language":"en"}}]""";

    /// <summary>
    /// Answers the translator page with a page carrying credentials and every
    /// other request with the given answer, handing each request to the observer.
    /// </summary>
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
        using BingTranslationProvider provider = new(new HttpClient());

        Assert.Equal("Bing", provider.Name);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutSourceLanguage_ReportsTheDetectedOne()
    {
        using StubHttpMessageHandler handler = Answering(Answer);
        using HttpClient httpClient = new(handler);
        using BingTranslationProvider provider = new(httpClient);

        TextTranslationRequest request = new(new ProviderText("Good morning"), new LanguageId("russian"));
        TextTranslationResult result = await provider.ExecuteAsync(request);

        Assert.Equal("Доброе утро", result.TranslatedText.Value);
        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.Equal("russian", result.TargetLanguageId.Value);
        Assert.True(result.WasSourceLanguageDetected);
        Assert.Equal(request.Id, result.RequestId);
    }

    [Fact]
    public async Task ExecuteAsync_ReadsTheCredentialsFromThePageAndSendsThem()
    {
        List<string> bodies = [];

        using StubHttpMessageHandler handler = Answering(Answer, request =>
        {
            if (request.Content is not null)
            {
                bodies.Add(request.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            }
        });

        using HttpClient httpClient = new(handler);
        using BingTranslationProvider provider = new(httpClient);

        await provider.ExecuteAsync(new TextTranslationRequest(new ProviderText("Good morning"), new LanguageId("russian")));

        string body = Assert.Single(bodies);

        Assert.Contains("token=the-token", body, StringComparison.Ordinal);
        Assert.Contains("key=9999999999999", body, StringComparison.Ordinal);
        Assert.Contains("fromLang=auto-detect", body, StringComparison.Ordinal);
        Assert.Contains("to=ru", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_SecondCall_ReusesTheCredentials()
    {
        List<Uri> uris = [];

        using StubHttpMessageHandler handler = Answering(Answer, request => uris.Add(request.RequestUri!));
        using HttpClient httpClient = new(handler);
        using BingTranslationProvider provider = new(httpClient);

        await provider.ExecuteAsync(new TextTranslationRequest(new ProviderText("Good morning"), new LanguageId("russian")));
        await provider.ExecuteAsync(new TextTranslationRequest(new ProviderText("Good evening"), new LanguageId("russian")));

        Assert.Equal(3, uris.Count);
        Assert.Single(uris, uri => uri.AbsolutePath.EndsWith("/translator", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_WithSourceLanguage_NamesIt()
    {
        List<string> bodies = [];

        using StubHttpMessageHandler handler = Answering(Answer, request =>
        {
            if (request.Content is not null)
            {
                bodies.Add(request.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            }
        });

        using HttpClient httpClient = new(handler);
        using BingTranslationProvider provider = new(httpClient);

        TextTranslationRequest request = new(
            new ProviderText("Good morning"),
            new LanguageId("russian"),
            new LanguageId("english"));

        TextTranslationResult result = await provider.ExecuteAsync(request);

        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.False(result.WasSourceLanguageDetected);
        Assert.Contains("fromLang=en", Assert.Single(bodies), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_LanguageWithItsOwnCode_SendsThatCode()
    {
        List<string> bodies = [];

        using StubHttpMessageHandler handler = Answering(Answer, request =>
        {
            if (request.Content is not null)
            {
                bodies.Add(request.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            }
        });

        using HttpClient httpClient = new(handler);
        using BingTranslationProvider provider = new(httpClient);

        await provider.ExecuteAsync(new TextTranslationRequest(new ProviderText("Good morning"), new LanguageId("chinese")));

        Assert.Contains("to=zh-Hans", Assert.Single(bodies), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_RefusalInsideTheAnswer_ThrowsProviderExceptionWithItsMessage()
    {
        using StubHttpMessageHandler handler = Answering(
            """[{"statusCode":400,"errorMessage":"The target language is not valid"}]""");

        using HttpClient httpClient = new(handler);
        using BingTranslationProvider provider = new(httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new TextTranslationRequest(new ProviderText("Good morning"), new LanguageId("russian"))));

        Assert.Equal("Bing", exception.ProviderName);
        Assert.Contains("not valid", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyAnswer_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering("[]");
        using HttpClient httpClient = new(handler);
        using BingTranslationProvider provider = new(httpClient);

        await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new TextTranslationRequest(new ProviderText("Good morning"), new LanguageId("russian"))));
    }

    [Fact]
    public async Task ExecuteAsync_NoDetectedLanguage_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering("""[{"translations":[{"text":"Доброе утро","to":"ru"}]}]""");
        using HttpClient httpClient = new(handler);
        using BingTranslationProvider provider = new(httpClient);

        await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new TextTranslationRequest(new ProviderText("Good morning"), new LanguageId("russian"))));
    }

    [Fact]
    public async Task ExecuteAsync_PageWithoutCredentials_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<html>nothing here</html>", Encoding.UTF8, "text/html"),
        });

        using HttpClient httpClient = new(handler);
        using BingTranslationProvider provider = new(httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new TextTranslationRequest(new ProviderText("Good morning"), new LanguageId("russian"))));

        Assert.Contains("no credentials", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_TooLongText_ThrowsArgumentException()
    {
        using StubHttpMessageHandler handler = Answering(Answer);
        using HttpClient httpClient = new(handler);
        using BingTranslationProvider provider = new(httpClient);

        await Assert.ThrowsAsync<ArgumentException>(
            () => provider.ExecuteAsync(new TextTranslationRequest(new ProviderText(new string('a', 1001)), new LanguageId("russian"))));
    }

    [Fact]
    public async Task ExecuteAsync_UnknownLanguage_ThrowsProviderException()
    {
        using BingTranslationProvider provider = new(new HttpClient());

        await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new TextTranslationRequest(new ProviderText("Good morning"), new LanguageId("does_not_exist"))));
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        using BingTranslationProvider provider = new(new HttpClient());

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.ExecuteAsync(null!));
    }

    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new BingTranslationProvider(null!));
    }

    [Fact]
    public void Dispose_Twice_DoesNotThrow()
    {
        BingTranslationProvider provider = new(new HttpClient());

        provider.Dispose();

        Assert.Null(Record.Exception(provider.Dispose));
    }

    [LiveFact]
    public async Task ExecuteAsync_LiveBing_TranslatesText()
    {
        using BingTranslationProvider provider = new();

        TextTranslationRequest request = new(new ProviderText("Good morning!"), new LanguageId("russian"));
        TextTranslationResult result = await provider.ExecuteAsync(request);

        Assert.False(string.IsNullOrWhiteSpace(result.TranslatedText.Value));
        Assert.Equal("english", result.SourceLanguageId.Value);
    }
}
