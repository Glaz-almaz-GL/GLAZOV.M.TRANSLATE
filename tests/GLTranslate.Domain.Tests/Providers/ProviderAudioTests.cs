using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.TextToSpeech;

namespace GLTranslate.Domain.Tests.Providers;

/// <summary>
/// Verifies what a recording handed to a provider is made of and what it
/// refuses.
/// </summary>
public sealed class ProviderAudioTests
{
    private static readonly AudioContentType Wav = new("audio/wav");

    [Fact]
    public void Constructor_KeepsTheBytesAndTheContentType()
    {
        ProviderAudio audio = new([1, 2, 3], Wav);

        Assert.Equal([1, 2, 3], audio.Content.ToArray());
        Assert.Equal(Wav, audio.ContentType);
    }

    [Fact]
    public void Constructor_CopiesTheBytes()
    {
        byte[] bytes = [1, 2, 3];
        ProviderAudio audio = new(bytes, Wav);

        bytes[0] = 9;

        Assert.Equal(1, audio.Content[0]);
    }

    [Fact]
    public void Constructor_EmptyContent_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new ProviderAudio([], Wav));
    }

    [Fact]
    public void Constructor_NullContentType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ProviderAudio([1], null!));
    }
}
