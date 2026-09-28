using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;
using System.Net;

namespace GLTranslate.Providers.Microsoft.Tests;

/// <summary>
/// Verifies the provider from the request it is given to the result it returns.
/// </summary>
public sealed class MicrosoftTranslationProviderTests
{
    private const string Answer =
        """[{"detectedLanguage":{"language":"en","score":0.99},"translations":[{"text":"Доброе утро","to":"ru"}]}]""";

    private static StubHttpMessageHandler Answering(string json)
    {
        return new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        });
    }

    [Fact]
    public void Name_IsMicrosoft()
    {
        using MicrosoftTranslationProvider provider = new(new HttpClient());

        Assert.Equal("Microsoft", provider.Name);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutSourceLanguage_ReportsTheDetectedOne()
    {
        using StubHttpMessageHandler handler = Answering(Answer);
        using HttpClient httpClient = new(handler);
        using MicrosoftTranslationProvider provider = new(httpClient);

        TextTranslationRequest request = new(new ProviderText("Good morning"), new LanguageId("russian"));
        TextTranslationResult result = await provider.ExecuteAsync(request);

        Assert.Equal("Доброе утро", result.TranslatedText.Value);
        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.Equal("russian", result.TargetLanguageId.Value);
        Assert.True(result.WasSourceLanguageDetected);
        Assert.Equal(request.Id, result.RequestId);
    }

    [Fact]
    public async Task ExecuteAsync_WithSourceLanguage_KeepsIt()
    {
        using StubHttpMessageHandler handler = Answering("""[{"translations":[{"text":"Доброе утро","to":"ru"}]}]""");
        using HttpClient httpClient = new(handler);
        using MicrosoftTranslationProvider provider = new(httpClient);

        TextTranslationRequest request = new(
            new ProviderText("Good morning"),
            new LanguageId("russian"),
            new LanguageId("english"));

        TextTranslationResult result = await provider.ExecuteAsync(request);

        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.False(result.WasSourceLanguageDetected);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownLanguage_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering(Answer);
        using HttpClient httpClient = new(handler);
        using MicrosoftTranslationProvider provider = new(httpClient);

        TextTranslationRequest request = new(new ProviderText("Good morning"), new LanguageId("does_not_exist"));

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(request));

        Assert.Equal("Microsoft", exception.ProviderName);
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        using MicrosoftTranslationProvider provider = new(new HttpClient());

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.ExecuteAsync(null!));
    }

    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new MicrosoftTranslationProvider(null!));
    }

    [Fact]
    public void Dispose_Twice_DoesNotThrow()
    {
        MicrosoftTranslationProvider provider = new(new HttpClient());

        provider.Dispose();

        Assert.Null(Record.Exception(provider.Dispose));
    }

    [LiveFact]
    public async Task ExecuteAsync_LiveMicrosoftTranslator_TranslatesText()
    {
        using MicrosoftTranslationProvider provider = new();

        TextTranslationRequest request = new(new ProviderText("Good morning!"), new LanguageId("russian"));
        TextTranslationResult result = await provider.ExecuteAsync(request);

        Assert.False(string.IsNullOrWhiteSpace(result.TranslatedText.Value));
        Assert.Equal("english", result.SourceLanguageId.Value);
    }
}
