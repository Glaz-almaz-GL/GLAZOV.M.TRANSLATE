using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.TextToSpeech;
using System.Net;

namespace GLTranslate.Providers.Google.Tests;

public sealed class GoogleTextToSpeechProviderTests
{
    [Fact]
    public void Name_ReturnsGoogle()
    {
        using GoogleTextToSpeechProvider provider = new();

        Assert.Equal("Google", provider.Name);
    }

    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new GoogleTextToSpeechProvider(null!));
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        using GoogleTextToSpeechProvider provider = new();

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsAudioDataWithMp3ContentType()
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([0xFF, 0xF3, 0x84, 0xC4]),
        });
        using HttpClient httpClient = new(handler);
        using GoogleTextToSpeechProvider provider = new(httpClient);

        TextToSpeechRequest request = new(new ProviderText("Hello"), new LanguageId("english"));

        TextToSpeechResult result = await provider.ExecuteAsync(request);

        Assert.Equal(request.Id, result.RequestId);
        Assert.Equal(new byte[] { 0xFF, 0xF3, 0x84, 0xC4 }, result.AudioData.ToArray());
        Assert.Equal("audio/mpeg", result.ContentType.Value);
        Assert.Equal("english", result.LanguageId.Value);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownLanguage_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using HttpClient httpClient = new(handler);
        using GoogleTextToSpeechProvider provider = new(httpClient);

        TextToSpeechRequest request = new(new ProviderText("Hello"), new LanguageId("does_not_exist"));

        await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(request));
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        GoogleTextToSpeechProvider provider = new();
        provider.Dispose();

        Exception? exception = Record.Exception(provider.Dispose);

        Assert.Null(exception);
    }

    [Fact]
    public async Task ExecuteAsync_NamedVoice_ThrowsProviderException()
    {
        using GoogleTextToSpeechProvider provider = new(new HttpClient());

        TextToSpeechRequest request = new(
            new ProviderText("Good morning"),
            new LanguageId("english"),
            new VoiceName("en-US-AvaNeural"));

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(request));

        Assert.Contains("offers no choice of voice", exception.Message, StringComparison.Ordinal);
    }
}
