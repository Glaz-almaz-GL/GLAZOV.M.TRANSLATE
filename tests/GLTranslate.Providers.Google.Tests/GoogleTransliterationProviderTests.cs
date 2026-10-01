using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Transliteration;
using System.Net;
using System.Text;

namespace GLTranslate.Providers.Google.Tests;

public sealed class GoogleTransliterationProviderTests
{
    [Fact]
    public void Name_ReturnsGoogle()
    {
        using GoogleTransliterationProvider provider = new();

        Assert.Equal("Google", provider.Name);
    }

    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new GoogleTransliterationProvider(null!));
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        using GoogleTransliterationProvider provider = new();

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsync_ExplicitLanguage_ReturnsResultWithoutDetectionFlag()
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"sentences":[{"trans":"Hello, world!","orig":"Привет, мир!"},{"src_translit":"Privet, mir!"}],"src":"ru"}""",
                Encoding.UTF8,
                "application/json"),
        });
        using HttpClient httpClient = new(handler);
        using GoogleTransliterationProvider provider = new(httpClient);

        TransliterationRequest request = new(new ProviderText("Привет, мир!"), new LanguageId("russian"));

        TransliterationResult result = await provider.ExecuteAsync(request);

        Assert.Equal(request.Id, result.RequestId);
        Assert.Equal("Privet, mir!", result.Transliteration.Value);
        Assert.Equal("russian", result.LanguageId.Value);
        Assert.False(result.WasLanguageDetected);
    }

    [Fact]
    public async Task ExecuteAsync_AutoDetectLanguage_ReturnsDetectedLanguageAndFlag()
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"sentences":[{"trans":"Hello World","orig":"こんにちは世界"},{"src_translit":"Kon'nichiwa sekai"}],"src":"ja"}""",
                Encoding.UTF8,
                "application/json"),
        });
        using HttpClient httpClient = new(handler);
        using GoogleTransliterationProvider provider = new(httpClient);

        TransliterationRequest request = new(new ProviderText("こんにちは世界"));

        TransliterationResult result = await provider.ExecuteAsync(request);

        Assert.Equal("japanese", result.LanguageId.Value);
        Assert.True(result.WasLanguageDetected);
    }

    [Fact]
    public async Task ExecuteAsync_NoTransliterationInResponse_FallsBackToOriginalText()
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"sentences":[{"trans":"Hello world","orig":"Hello world"}],"src":"en"}""",
                Encoding.UTF8,
                "application/json"),
        });
        using HttpClient httpClient = new(handler);
        using GoogleTransliterationProvider provider = new(httpClient);

        TransliterationRequest request = new(new ProviderText("Hello world"), new LanguageId("english"));

        TransliterationResult result = await provider.ExecuteAsync(request);

        Assert.Equal("Hello world", result.Transliteration.Value);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        GoogleTransliterationProvider provider = new();
        provider.Dispose();

        Exception? exception = Record.Exception(provider.Dispose);

        Assert.Null(exception);
    }
}
