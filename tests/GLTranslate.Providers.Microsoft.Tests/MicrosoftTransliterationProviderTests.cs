using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Transliteration;
using System.Net;

namespace GLTranslate.Providers.Microsoft.Tests;

/// <summary>
/// Verifies the transliteration provider from the request it is given to the
/// result it returns.
/// </summary>
public sealed class MicrosoftTransliterationProviderTests
{
    private const string Answer = """[{"text":"Dobroye utro","script":"Latn"}]""";

    private static StubHttpMessageHandler Answering(string json, Action<HttpRequestMessage>? observe = null)
    {
        return new StubHttpMessageHandler(request =>
        {
            observe?.Invoke(request);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
            };
        });
    }

    [Fact]
    public void Name_IsMicrosoft()
    {
        using MicrosoftTransliterationProvider provider = new(new HttpClient());

        Assert.Equal("Microsoft", provider.Name);
    }

    [Fact]
    public async Task ExecuteAsync_Answer_ReturnsTransliteration()
    {
        using StubHttpMessageHandler handler = Answering(Answer);
        using HttpClient httpClient = new(handler);
        using MicrosoftTransliterationProvider provider = new(httpClient);

        TransliterationRequest request = new(new ProviderText("Доброе утро"), new LanguageId("russian"));
        TransliterationResult result = await provider.ExecuteAsync(request);

        Assert.Equal("Dobroye utro", result.Transliteration.Value);
        Assert.Equal("russian", result.LanguageId.Value);
        Assert.False(result.WasLanguageDetected);
        Assert.Equal(request.Id, result.RequestId);
    }

    [Fact]
    public async Task ExecuteAsync_NamesTheScriptsOfTheLanguage()
    {
        Uri? requestUri = null;

        using StubHttpMessageHandler handler = Answering(Answer, request => requestUri = request.RequestUri);
        using HttpClient httpClient = new(handler);
        using MicrosoftTransliterationProvider provider = new(httpClient);

        await provider.ExecuteAsync(new TransliterationRequest(new ProviderText("Доброе утро"), new LanguageId("russian")));

        Assert.Contains("language=ru", requestUri!.Query, StringComparison.Ordinal);
        Assert.Contains("fromScript=Cyrl", requestUri.Query, StringComparison.Ordinal);
        Assert.Contains("toScript=Latn", requestUri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutLanguage_ThrowsProviderException()
    {
        using MicrosoftTransliterationProvider provider = new(new HttpClient());

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new TransliterationRequest(new ProviderText("Доброе утро"))));

        Assert.Equal("Microsoft", exception.ProviderName);
        Assert.Contains("cannot detect the language", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_LatinLanguage_ThrowsProviderException()
    {
        using MicrosoftTransliterationProvider provider = new(new HttpClient());

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new TransliterationRequest(new ProviderText("Good morning"), new LanguageId("english"))));

        Assert.Contains("already written in the Latin script", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownLanguage_ThrowsProviderException()
    {
        using MicrosoftTransliterationProvider provider = new(new HttpClient());

        await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new TransliterationRequest(new ProviderText("Доброе утро"), new LanguageId("does_not_exist"))));
    }

    [Fact]
    public async Task ExecuteAsync_NoTransliteration_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering("[]");
        using HttpClient httpClient = new(handler);
        using MicrosoftTransliterationProvider provider = new(httpClient);

        await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new TransliterationRequest(new ProviderText("Доброе утро"), new LanguageId("russian"))));
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        using MicrosoftTransliterationProvider provider = new(new HttpClient());

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.ExecuteAsync(null!));
    }

    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new MicrosoftTransliterationProvider(null!));
    }

    [LiveFact]
    public async Task ExecuteAsync_LiveMicrosoftTranslator_TransliteratesText()
    {
        using MicrosoftTransliterationProvider provider = new();

        TransliterationRequest request = new(new ProviderText("Доброе утро"), new LanguageId("russian"));
        TransliterationResult result = await provider.ExecuteAsync(request);

        Assert.False(string.IsNullOrWhiteSpace(result.Transliteration.Value));
        Assert.Equal("russian", result.LanguageId.Value);
    }
}
