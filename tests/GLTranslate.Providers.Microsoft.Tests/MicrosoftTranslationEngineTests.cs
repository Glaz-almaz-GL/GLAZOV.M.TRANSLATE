using GLTranslate.Abstractions.Providers;
using GLTranslate.Providers.Microsoft.Internal;
using System.Net;

namespace GLTranslate.Providers.Microsoft.Tests;

/// <summary>
/// Verifies the request the engine builds and how it reads the answer.
/// </summary>
public sealed class MicrosoftTranslationEngineTests
{
    private const string Answer =
        """[{"detectedLanguage":{"language":"en","score":0.99},"translations":[{"text":"Доброе утро","to":"ru"}]}]""";

    private static StubHttpMessageHandler Answering(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        return new StubHttpMessageHandler(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        });
    }

    [Fact]
    public async Task TranslateAsync_Answer_ReturnsTranslationAndDetectedLanguage()
    {
        using StubHttpMessageHandler handler = Answering(Answer);
        using HttpClient httpClient = new(handler);
        using MicrosoftTranslationEngine engine = new(httpClient);

        (string translatedText, string sourceLanguageCode) = await engine.TranslateAsync("Good morning", "ru", null);

        Assert.Equal("Доброе утро", translatedText);
        Assert.Equal("en", sourceLanguageCode);
    }

    [Fact]
    public async Task TranslateAsync_KnownSourceLanguage_KeepsItAndNamesItInTheRequest()
    {
        Uri? requestUri = null;

        using StubHttpMessageHandler handler = new(request =>
        {
            requestUri = request.RequestUri;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""[{"translations":[{"text":"Доброе утро","to":"ru"}]}]""", System.Text.Encoding.UTF8, "application/json"),
            };
        });

        using HttpClient httpClient = new(handler);
        using MicrosoftTranslationEngine engine = new(httpClient);

        (_, string sourceLanguageCode) = await engine.TranslateAsync("Good morning", "ru", "en");

        Assert.Equal("en", sourceLanguageCode);
        Assert.Contains("from=en", requestUri!.Query, StringComparison.Ordinal);
        Assert.Contains("to=ru", requestUri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TranslateAsync_SignsEveryRequest()
    {
        string? signature = null;

        using StubHttpMessageHandler handler = new(request =>
        {
            signature = request.Headers.GetValues("X-MT-Signature").Single();

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Answer, System.Text.Encoding.UTF8, "application/json"),
            };
        });

        using HttpClient httpClient = new(handler);
        using MicrosoftTranslationEngine engine = new(httpClient);

        await engine.TranslateAsync("Good morning", "ru", null);

        Assert.StartsWith("MSTranslatorAndroidApp::", signature, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TranslateAsync_FailedRequest_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering("{}", HttpStatusCode.ServiceUnavailable);
        using HttpClient httpClient = new(handler);
        using MicrosoftTranslationEngine engine = new(httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(
            () => engine.TranslateAsync("Good morning", "ru", null));

        Assert.Equal("Microsoft", exception.ProviderName);
    }

    [Fact]
    public async Task TranslateAsync_EmptyAnswer_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering("[]");
        using HttpClient httpClient = new(handler);
        using MicrosoftTranslationEngine engine = new(httpClient);

        await Assert.ThrowsAsync<ProviderException>(() => engine.TranslateAsync("Good morning", "ru", null));
    }

    [Fact]
    public async Task TranslateAsync_NoTranslation_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering("""[{"translations":[]}]""");
        using HttpClient httpClient = new(handler);
        using MicrosoftTranslationEngine engine = new(httpClient);

        await Assert.ThrowsAsync<ProviderException>(() => engine.TranslateAsync("Good morning", "ru", null));
    }

    [Fact]
    public async Task TranslateAsync_NoDetectedLanguage_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering("""[{"translations":[{"text":"Доброе утро","to":"ru"}]}]""");
        using HttpClient httpClient = new(handler);
        using MicrosoftTranslationEngine engine = new(httpClient);

        await Assert.ThrowsAsync<ProviderException>(() => engine.TranslateAsync("Good morning", "ru", null));
    }

    [Fact]
    public async Task TranslateAsync_UnreadableAnswer_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering("not json at all");
        using HttpClient httpClient = new(handler);
        using MicrosoftTranslationEngine engine = new(httpClient);

        await Assert.ThrowsAsync<ProviderException>(() => engine.TranslateAsync("Good morning", "ru", null));
    }

    [Fact]
    public async Task TranslateAsync_TooLongText_ThrowsArgumentException()
    {
        using StubHttpMessageHandler handler = Answering(Answer);
        using HttpClient httpClient = new(handler);
        using MicrosoftTranslationEngine engine = new(httpClient);

        await Assert.ThrowsAsync<ArgumentException>(
            () => engine.TranslateAsync(new string('a', 1001), "ru", null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TranslateAsync_EmptyText_ThrowsArgumentException(string text)
    {
        using StubHttpMessageHandler handler = Answering(Answer);
        using HttpClient httpClient = new(handler);
        using MicrosoftTranslationEngine engine = new(httpClient);

        await Assert.ThrowsAsync<ArgumentException>(() => engine.TranslateAsync(text, "ru", null));
    }

    [Fact]
    public async Task TranslateAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        using StubHttpMessageHandler handler = Answering(Answer);
        using HttpClient httpClient = new(handler);
        MicrosoftTranslationEngine engine = new(httpClient);

        engine.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => engine.TranslateAsync("Good morning", "ru", null));
    }

    [Fact]
    public void Dispose_ExternalHttpClient_LeavesItUsable()
    {
        using StubHttpMessageHandler handler = Answering(Answer);
        using HttpClient httpClient = new(handler);
        MicrosoftTranslationEngine engine = new(httpClient);

        engine.Dispose();

        Assert.Null(Record.Exception(() => httpClient.Timeout));
    }

    [Fact]
    public void Dispose_Twice_DoesNotThrow()
    {
        MicrosoftTranslationEngine engine = new();

        engine.Dispose();

        Assert.Null(Record.Exception(engine.Dispose));
    }
}
