using GLAZOV.M.TRANSLATE.Abstractions.TextToSpeech;

namespace GLAZOV.M.TRANSLATE.Abstractions.Translation;

/// <summary>
/// Represents a translation spoken aloud by a provider.
/// </summary>
/// <param name="AudioData">
/// The bytes of the recording of the translation.
/// </param>
/// <param name="ContentType">
/// The content type of the recording, such as <c>audio/mpeg</c>.
/// </param>
public readonly record struct SpokenTranslation(ReadOnlyMemory<byte> AudioData, AudioContentType ContentType);
