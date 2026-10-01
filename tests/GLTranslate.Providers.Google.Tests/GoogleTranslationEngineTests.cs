using GLTranslate.Abstractions.Providers;
using GLTranslate.Providers.Google.Internal;
using System.Net;
using System.Text;

namespace GLTranslate.Providers.Google.Tests;

public sealed class GoogleTranslationEngineTests
{
    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new GoogleTranslationEngine(null!));
    }

    [Fact]
    public async Task TranslateAsync_SuccessfulResponse_ConcatenatesSentencesAndReturnsSource()
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"sentences":[{"trans":"Привет"},{"trans":", мир!"}],"src":"en"}""",
                Encoding.UTF8,
                "application/json"),
        });
        using HttpClient httpClient = new(handler);
        GoogleTranslationEngine engine = new(httpClient);

        (string translatedText, string sourceLanguageCode) = await engine.TranslateAsync("Hello, world!", "en", "ru");

        Assert.Equal("Привет, мир!", translatedText);
        Assert.Equal("en", sourceLanguageCode);
    }

    [Fact]
    public async Task TranslateAsync_NoSentences_ReturnsEmptyTranslation()
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"src":"en"}""", Encoding.UTF8, "application/json"),
        });
        using HttpClient httpClient = new(handler);
        GoogleTranslationEngine engine = new(httpClient);

        (string translatedText, string sourceLanguageCode) = await engine.TranslateAsync("Hello", "en", "ru");

        Assert.Equal(string.Empty, translatedText);
        Assert.Equal("en", sourceLanguageCode);
    }

    [Fact]
    public async Task TranslateAsync_UnsuccessfulStatusCode_ThrowsTranslationProviderException()
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        using HttpClient httpClient = new(handler);
        GoogleTranslationEngine engine = new(httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(
            () => engine.TranslateAsync("Hello", "en", "ru"));

        Assert.Equal("Google", exception.ProviderName);
    }

    [Fact]
    public async Task TranslateAsync_MalformedJson_ThrowsTranslationProviderException()
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not json", Encoding.UTF8, "application/json"),
        });
        using HttpClient httpClient = new(handler);
        GoogleTranslationEngine engine = new(httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(
            () => engine.TranslateAsync("Hello", "en", "ru"));

        Assert.Equal("Google", exception.ProviderName);
    }

    [Theory]
    [InlineData("", "en", "ru")]
    [InlineData("Hello", "", "ru")]
    [InlineData("Hello", "en", "")]
    public async Task TranslateAsync_EmptyArgument_ThrowsArgumentException(string text, string source, string target)
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using HttpClient httpClient = new(handler);
        GoogleTranslationEngine engine = new(httpClient);

        await Assert.ThrowsAsync<ArgumentException>(() => engine.TranslateAsync(text, source, target));
    }

    [Fact]
    public async Task TransliterateAsync_SourceTransliterationPresent_ReturnsIt()
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"sentences":[{"trans":"Hello, world!","orig":"Привет, мир!"},{"src_translit":"Privet, mir!"}],"src":"ru"}""",
                Encoding.UTF8,
                "application/json"),
        });
        using HttpClient httpClient = new(handler);
        GoogleTranslationEngine engine = new(httpClient);

        (string transliteration, string sourceLanguageCode) = await engine.TransliterateAsync("Привет, мир!", "ru");

        Assert.Equal("Privet, mir!", transliteration);
        Assert.Equal("ru", sourceLanguageCode);
    }

    [Fact]
    public async Task TransliterateAsync_NoSourceTransliteration_FallsBackToOriginalText()
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"sentences":[{"trans":"Hello world","orig":"Hello world"}],"src":"en"}""",
                Encoding.UTF8,
                "application/json"),
        });
        using HttpClient httpClient = new(handler);
        GoogleTranslationEngine engine = new(httpClient);

        (string transliteration, string sourceLanguageCode) = await engine.TransliterateAsync("Hello world", "en");

        Assert.Equal("Hello world", transliteration);
        Assert.Equal("en", sourceLanguageCode);
    }
}
