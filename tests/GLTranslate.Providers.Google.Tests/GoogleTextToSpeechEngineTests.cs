using GLTranslate.Abstractions.Providers;
using GLTranslate.Providers.Google.Internal;
using System.Net;

namespace GLTranslate.Providers.Google.Tests;

public sealed class GoogleTextToSpeechEngineTests
{
    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new GoogleTextToSpeechEngine(null!));
    }

    [Theory]
    [InlineData("", "en")]
    [InlineData("Hello", "")]
    public async Task SynthesizeAsync_EmptyArgument_ThrowsArgumentException(string text, string languageCode)
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using HttpClient httpClient = new(handler);
        GoogleTextToSpeechEngine engine = new(httpClient);

        await Assert.ThrowsAsync<ArgumentException>(() => engine.SynthesizeAsync(text, languageCode));
    }

    [Fact]
    public async Task SynthesizeAsync_ShortText_SendsSingleRequestAndReturnsAudioBytes()
    {
        int requestCount = 0;
        using StubHttpMessageHandler handler = new(_ =>
        {
            requestCount++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([1, 2, 3]),
            };
        });
        using HttpClient httpClient = new(handler);
        GoogleTextToSpeechEngine engine = new(httpClient);

        byte[] audio = await engine.SynthesizeAsync("Hello, world!", "en");

        Assert.Equal(1, requestCount);
        Assert.Equal(new byte[] { 1, 2, 3 }, audio);
    }

    [Fact]
    public async Task SynthesizeAsync_LongText_SplitsIntoOrderedChunksAndConcatenates()
    {
        using StubHttpMessageHandler handler = new(request =>
        {
            int index = int.Parse(GetQueryParameter(request.RequestUri!, "idx"));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([(byte)index]),
            };
        });
        using HttpClient httpClient = new(handler);
        GoogleTextToSpeechEngine engine = new(httpClient);

        string longText = string.Join(" ", Enumerable.Repeat("word", 100));

        byte[] audio = await engine.SynthesizeAsync(longText, "en");

        Assert.True(audio.Length > 1, "Expected the text to be split into more than one chunk.");
        Assert.Equal(Enumerable.Range(0, audio.Length).Select(i => (byte)i), audio);
    }

    [Fact]
    public async Task SynthesizeAsync_UnsuccessfulStatusCode_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));
        using HttpClient httpClient = new(handler);
        GoogleTextToSpeechEngine engine = new(httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(
            () => engine.SynthesizeAsync("Hello", "en"));

        Assert.Equal("Google", exception.ProviderName);
    }

    private static string GetQueryParameter(Uri uri, string name)
    {
        foreach (string pair in uri.Query.TrimStart('?').Split('&'))
        {
            string[] parts = pair.Split('=', 2);

            if (parts[0] == name)
            {
                return Uri.UnescapeDataString(parts[1]);
            }
        }

        throw new InvalidOperationException($"Query parameter '{name}' was not found in '{uri}'.");
    }
}
