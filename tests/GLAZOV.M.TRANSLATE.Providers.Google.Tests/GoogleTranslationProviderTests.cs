using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Abstractions.Translation;
using System.Net;
using System.Text;

namespace GLAZOV.M.TRANSLATE.Providers.Google.Tests;

public sealed class GoogleTranslationProviderTests
{
    [Fact]
    public void Name_ReturnsGoogle()
    {
        using GoogleTranslationProvider provider = new();

        Assert.Equal("Google", provider.Name);
    }

    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new GoogleTranslationProvider(null!));
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        using GoogleTranslationProvider provider = new();

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsync_ExplicitSourceLanguage_ReturnsResultWithoutDetectionFlag()
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"sentences":[{"trans":"Bonjour"}],"src":"en"}""",
                Encoding.UTF8,
                "application/json"),
        });
        using HttpClient httpClient = new(handler);
        using GoogleTranslationProvider provider = new(httpClient);

        TextTranslationRequest request = new(
            new ProviderText("Hello"),
            new LanguageId("french"),
            new LanguageId("english"));

        TextTranslationResult result = await provider.ExecuteAsync(request);

        Assert.Equal(request.Id, result.RequestId);
        Assert.Equal("Bonjour", result.TranslatedText.Value);
        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.Equal("french", result.TargetLanguageId.Value);
        Assert.False(result.WasSourceLanguageDetected);
    }

    [Fact]
    public async Task ExecuteAsync_AutoDetectSource_ReturnsDetectedLanguageAndFlag()
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"sentences":[{"trans":"Привет"}],"src":"en"}""",
                Encoding.UTF8,
                "application/json"),
        });
        using HttpClient httpClient = new(handler);
        using GoogleTranslationProvider provider = new(httpClient);

        TextTranslationRequest request = new(new ProviderText("Hello"), new LanguageId("russian"));

        TextTranslationResult result = await provider.ExecuteAsync(request);

        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.True(result.WasSourceLanguageDetected);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        GoogleTranslationProvider provider = new();
        provider.Dispose();

        Exception? exception = Record.Exception(provider.Dispose);

        Assert.Null(exception);
    }

    [LiveFact]
    public async Task ExecuteAsync_LiveGoogleTranslate_TranslatesText()
    {
        using GoogleTranslationProvider provider = new();

        TextTranslationRequest request = new(new ProviderText("Good morning!"), new LanguageId("russian"));
        TextTranslationResult result = await provider.ExecuteAsync(request);

        Assert.False(string.IsNullOrWhiteSpace(result.TranslatedText.Value));
        Assert.Equal("english", result.SourceLanguageId.Value);
    }
}
