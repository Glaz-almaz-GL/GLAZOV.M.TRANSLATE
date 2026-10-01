using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Abstractions.Translation;
using System.Net;
using System.Text;

namespace GLAZOV.M.TRANSLATE.Providers.Yandex.Tests;

/// <summary>
/// Verifies the translation provider from the request it is given to the
/// result it returns.
/// </summary>
public sealed class YandexTranslationProviderTests
{
    private const string Answer = """{"code":200,"lang":"en-ru","text":["Доброе утро"]}""";

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
        using YandexTranslationProvider provider = new(new HttpClient());

        Assert.Equal("Yandex", provider.Name);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutSourceLanguage_ReportsTheDetectedOne()
    {
        using StubHttpMessageHandler handler = Answering(Answer);
        using HttpClient httpClient = new(handler);
        using YandexTranslationProvider provider = new(httpClient);

        TextTranslationRequest request = new(new ProviderText("Good morning"), new LanguageId("russian"));
        TextTranslationResult result = await provider.ExecuteAsync(request);

        Assert.Equal("Доброе утро", result.TranslatedText.Value);
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
        using YandexTranslationProvider provider = new(httpClient);

        TextTranslationRequest request = new(
            new ProviderText("Good morning"),
            new LanguageId("russian"),
            new LanguageId("english"));

        TextTranslationResult result = await provider.ExecuteAsync(request);

        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.False(result.WasSourceLanguageDetected);
        Assert.Contains("lang=en-ru", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutSourceLanguage_SendsTheTargetAlone()
    {
        string? body = null;

        using StubHttpMessageHandler handler = Answering(
            Answer,
            observe: request => body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());

        using HttpClient httpClient = new(handler);
        using YandexTranslationProvider provider = new(httpClient);

        await provider.ExecuteAsync(new TextTranslationRequest(new ProviderText("Good morning"), new LanguageId("russian")));

        Assert.Contains("lang=ru", body, StringComparison.Ordinal);
        Assert.DoesNotContain("lang=en-ru", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_RefusalInsideTheAnswer_ThrowsProviderExceptionWithItsMessage()
    {
        using StubHttpMessageHandler handler = Answering(
            """{"code":501,"message":"The specified translation direction is not supported"}""",
            HttpStatusCode.BadRequest);

        using HttpClient httpClient = new(handler);
        using YandexTranslationProvider provider = new(httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new TextTranslationRequest(new ProviderText("Good morning"), new LanguageId("russian"))));

        Assert.Equal("Yandex", exception.ProviderName);
        Assert.Contains("not supported", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_NoTranslation_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering("""{"code":200,"lang":"en-ru","text":[]}""");
        using HttpClient httpClient = new(handler);
        using YandexTranslationProvider provider = new(httpClient);

        await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new TextTranslationRequest(new ProviderText("Good morning"), new LanguageId("russian"))));
    }

    [Fact]
    public async Task ExecuteAsync_NoDirection_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering("""{"code":200,"text":["Доброе утро"]}""");
        using HttpClient httpClient = new(handler);
        using YandexTranslationProvider provider = new(httpClient);

        await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new TextTranslationRequest(new ProviderText("Good morning"), new LanguageId("russian"))));
    }

    [Fact]
    public async Task ExecuteAsync_UnreadableAnswer_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering("not json at all");
        using HttpClient httpClient = new(handler);
        using YandexTranslationProvider provider = new(httpClient);

        await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new TextTranslationRequest(new ProviderText("Good morning"), new LanguageId("russian"))));
    }

    [Fact]
    public async Task ExecuteAsync_UnknownLanguage_ThrowsProviderException()
    {
        using YandexTranslationProvider provider = new(new HttpClient());

        await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new TextTranslationRequest(new ProviderText("Good morning"), new LanguageId("does_not_exist"))));
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        using YandexTranslationProvider provider = new(new HttpClient());

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.ExecuteAsync(null!));
    }

    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new YandexTranslationProvider(null!));
    }

    [Fact]
    public void Dispose_Twice_DoesNotThrow()
    {
        YandexTranslationProvider provider = new(new HttpClient());

        provider.Dispose();

        Assert.Null(Record.Exception(provider.Dispose));
    }

    [LiveFact]
    public async Task ExecuteAsync_LiveYandex_TranslatesText()
    {
        using YandexTranslationProvider provider = new();

        TextTranslationRequest request = new(new ProviderText("Good morning!"), new LanguageId("russian"));
        TextTranslationResult result = await provider.ExecuteAsync(request);

        Assert.False(string.IsNullOrWhiteSpace(result.TranslatedText.Value));
        Assert.Equal("english", result.SourceLanguageId.Value);
    }
}
