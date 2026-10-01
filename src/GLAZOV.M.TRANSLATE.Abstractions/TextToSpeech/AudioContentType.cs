using GLAZOV.M.TRANSLATE.Abstractions.Common;

namespace GLAZOV.M.TRANSLATE.Abstractions.TextToSpeech;

/// <summary>
/// Represents the MIME content type of audio data, synthesized or recorded
/// (for example <c>"audio/mpeg"</c>).
/// </summary>
/// <remarks>
/// Instances of this class are immutable and thread-safe.
/// </remarks>
public sealed class AudioContentType(string value) : StringValueObject(value)
{
    /// <summary>
    /// Gets the content type of an MP3 recording, which is what most providers
    /// give back.
    /// </summary>
    public static AudioContentType Mp3 { get; } = new("audio/mpeg");
}
