using GLAZOV.M.TRANSLATE.Abstractions.TextToSpeech;

namespace GLAZOV.M.TRANSLATE.Domain.Tests.TextToSpeech;

/// <summary>
/// Verifies the content type of audio data.
/// </summary>
public sealed class AudioContentTypeTests
{
    [Fact]
    public void Mp3_IsAudioMpeg()
    {
        Assert.Equal("audio/mpeg", AudioContentType.Mp3.Value);
    }

    [Fact]
    public void Mp3_EqualsAContentTypeOfTheSameValue()
    {
        Assert.Equal(AudioContentType.Mp3, new AudioContentType("audio/mpeg"));
    }

    [Fact]
    public void Mp3_IsTheSameInstanceEveryTime()
    {
        Assert.Same(AudioContentType.Mp3, AudioContentType.Mp3);
    }
}
