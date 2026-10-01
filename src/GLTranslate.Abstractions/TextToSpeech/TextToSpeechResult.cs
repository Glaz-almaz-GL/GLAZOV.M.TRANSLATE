using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;

namespace GLTranslate.Abstractions.TextToSpeech;

/// <summary>
/// Represents the immutable outcome of a single text-to-speech operation.
/// </summary>
/// <remarks>
/// Instances of this class are immutable and thread-safe.
/// </remarks>
public sealed class TextToSpeechResult : ProviderResult
{
    /// <summary>
    /// Gets the synthesized audio data.
    /// </summary>
    public ReadOnlyMemory<byte> AudioData { get; }

    /// <summary>
    /// Gets the MIME content type of <see cref="AudioData"/>.
    /// </summary>
    public AudioContentType ContentType { get; }

    /// <summary>
    /// Gets the identifier of the language the synthesized text was
    /// written in.
    /// </summary>
    public LanguageId LanguageId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextToSpeechResult"/> class.
    /// </summary>
    /// <param name="requestId">
    /// The identifier of the <see cref="TextToSpeechRequest"/> this result
    /// was produced from.
    /// </param>
    /// <param name="audioData">
    /// The synthesized audio data.
    /// </param>
    /// <param name="contentType">
    /// The MIME content type of <paramref name="audioData"/>.
    /// </param>
    /// <param name="languageId">
    /// The identifier of the language the synthesized text was written in.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="contentType"/> or <paramref name="languageId"/>
    /// is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="audioData"/> is empty.
    /// </exception>
    public TextToSpeechResult(
        RequestId requestId,
        ReadOnlyMemory<byte> audioData,
        AudioContentType contentType,
        LanguageId languageId)
        : base(requestId)
    {
        ArgumentNullException.ThrowIfNull(contentType);
        ArgumentNullException.ThrowIfNull(languageId);

        if (audioData.IsEmpty)
        {
            // Empty audio data
            throw new ArgumentException("Audio data cannot be empty.", nameof(audioData));
        }

        AudioData = audioData;
        ContentType = contentType;
        LanguageId = languageId;
    }
}
