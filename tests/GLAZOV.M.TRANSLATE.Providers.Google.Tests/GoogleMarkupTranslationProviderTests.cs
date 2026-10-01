using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Abstractions.Translation;
using System.Net;
using System.Text;

namespace GLAZOV.M.TRANSLATE.Providers.Google.Tests;

/// <summary>
/// Verifies the markup translation provider: the request it builds, the two
/// shapes of answer the endpoint gives, and what it refuses.
/// </summary>
public sealed class GoogleMarkupTranslationProviderTests
{
    private const string Markup = "<p>Good <b>morning</b></p>";

    private const string Translated = "<p>Доброе <b>утро</b></p>";

    private static StubHttpMessageHandler Answering(string json, Action<HttpRequestMessage>? observe = null)
    {
        return new StubHttpMessageHandler(request =>
        {
            observe?.Invoke(request);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        });
    }

    [Fact]
    public void Name_IsGoogle()
    {
        using GoogleMarkupTranslationProvider provider = new(new HttpClient());

        Assert.Equal("Google", provider.Name);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutSourceLanguage_ReportsTheDetectedOne()
    {
        using StubHttpMessageHandler handler = Answering($"""[["{Translated}","en"]]""");
        using HttpClient httpClient = new(handler);
        using GoogleMarkupTranslationProvider provider = new(httpClient);

        MarkupTranslationRequest request = new(new ProviderMarkup(Markup), new LanguageId("russian"));
        MarkupTranslationResult result = await provider.ExecuteAsync(request);

        Assert.Equal(Translated, result.TranslatedMarkup.Value);
        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.Equal("russian", result.TargetLanguageId.Value);
        Assert.True(result.WasSourceLanguageDetected);
        Assert.Equal(request.Id, result.RequestId);
    }

    [Fact]
    public async Task ExecuteAsync_WithSourceLanguage_ReadsTheShorterAnswer()
    {
        using StubHttpMessageHandler handler = Answering($"""["{Translated}"]""");
        using HttpClient httpClient = new(handler);
        using GoogleMarkupTranslationProvider provider = new(httpClient);

        MarkupTranslationRequest request = new(
            new ProviderMarkup(Markup),
            new LanguageId("russian"),
            new LanguageId("english"));

        MarkupTranslationResult result = await provider.ExecuteAsync(request);

        Assert.Equal(Translated, result.TranslatedMarkup.Value);
        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.False(result.WasSourceLanguageDetected);
    }

    [Fact]
    public async Task ExecuteAsync_AsksTheMarkupEndpointForHtml()
    {
        Uri? requestUri = null;

        using StubHttpMessageHandler handler = Answering($"""[["{Translated}","en"]]""", request => requestUri = request.RequestUri);
        using HttpClient httpClient = new(handler);
        using GoogleMarkupTranslationProvider provider = new(httpClient);

        await provider.ExecuteAsync(new MarkupTranslationRequest(new ProviderMarkup(Markup), new LanguageId("russian")));

        Assert.EndsWith("/translate_a/t", requestUri!.AbsolutePath, StringComparison.Ordinal);
        Assert.Contains("format=html", requestUri.Query, StringComparison.Ordinal);
        Assert.Contains("sl=auto", requestUri.Query, StringComparison.Ordinal);
        Assert.Contains("tl=ru", requestUri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyAnswer_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering("[]");
        using HttpClient httpClient = new(handler);
        using GoogleMarkupTranslationProvider provider = new(httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new MarkupTranslationRequest(new ProviderMarkup(Markup), new LanguageId("russian"))));

        Assert.Equal("Google", exception.ProviderName);
    }

    [Fact]
    public async Task ExecuteAsync_AnswerOfAnotherShape_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering("""{"sentences":[]}""");
        using HttpClient httpClient = new(handler);
        using GoogleMarkupTranslationProvider provider = new(httpClient);

        await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new MarkupTranslationRequest(new ProviderMarkup(Markup), new LanguageId("russian"))));
    }

    [Fact]
    public async Task ExecuteAsync_NoDetectedLanguage_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering($"""["{Translated}"]""");
        using HttpClient httpClient = new(handler);
        using GoogleMarkupTranslationProvider provider = new(httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new MarkupTranslationRequest(new ProviderMarkup(Markup), new LanguageId("russian"))));

        Assert.Contains("detected no source language", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_FailedRequest_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        using HttpClient httpClient = new(handler);
        using GoogleMarkupTranslationProvider provider = new(httpClient);

        await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new MarkupTranslationRequest(new ProviderMarkup(Markup), new LanguageId("russian"))));
    }

    [Fact]
    public async Task ExecuteAsync_UnknownLanguage_ThrowsProviderException()
    {
        using GoogleMarkupTranslationProvider provider = new(new HttpClient());

        await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new MarkupTranslationRequest(new ProviderMarkup(Markup), new LanguageId("does_not_exist"))));
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        using GoogleMarkupTranslationProvider provider = new(new HttpClient());

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.ExecuteAsync(null!));
    }

    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new GoogleMarkupTranslationProvider(null!));
    }

    [Fact]
    public void Dispose_Twice_DoesNotThrow()
    {
        GoogleMarkupTranslationProvider provider = new(new HttpClient());

        provider.Dispose();

        Assert.Null(Record.Exception(provider.Dispose));
    }

    [LiveFact]
    public async Task ExecuteAsync_LiveGoogleTranslate_KeepsTheTags()
    {
        using GoogleMarkupTranslationProvider provider = new();

        MarkupTranslationResult result = await provider.ExecuteAsync(
            new MarkupTranslationRequest(new ProviderMarkup("<p>Good <b>morning</b>, world!</p>"), new LanguageId("russian")));

        Assert.StartsWith("<p>", result.TranslatedMarkup.Value, StringComparison.Ordinal);
        Assert.Contains("<b>", result.TranslatedMarkup.Value, StringComparison.Ordinal);
        Assert.EndsWith("</p>", result.TranslatedMarkup.Value, StringComparison.Ordinal);
        Assert.Equal("english", result.SourceLanguageId.Value);
    }
}
