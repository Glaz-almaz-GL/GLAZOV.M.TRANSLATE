using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;

namespace GLAZOV.M.TRANSLATE.Abstractions.Translation;

/// <summary>
/// Represents the immutable parameters of a single audio translation
/// operation.
/// </summary>
/// <remarks>
/// Instances of this class are immutable and thread-safe.
/// </remarks>
public sealed class AudioTranslationRequest : ProviderRequest
{
    /// <summary>
    /// Gets the recording to translate.
    /// </summary>
    public ProviderAudio Audio { get; }

    /// <summary>
    /// Gets the identifier of the language to translate into.
    /// </summary>
    public LanguageId TargetLanguageId { get; }

    /// <summary>
    /// Gets the identifier of the language spoken in the recording, or
    /// <see langword="null"/> to let the provider detect it. A provider that
    /// cannot detect the language of speech refuses a request without it.
    /// </summary>
    public LanguageId? SourceLanguageId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioTranslationRequest"/> class.
    /// </summary>
    /// <param name="audio">
    /// The recording to translate.
    /// </param>
    /// <param name="targetLanguageId">
    /// The identifier of the language to translate into.
    /// </param>
    /// <param name="sourceLanguageId">
    /// The identifier of the language spoken in the recording, or
    /// <see langword="null"/> to let the provider detect it.
    /// </param>
    /// <param name="id">
    /// The request identifier, or <see langword="null"/> to generate a new one.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="audio"/> or <paramref name="targetLanguageId"/>
    /// is <see langword="null"/>.
    /// </exception>
    public AudioTranslationRequest(
        ProviderAudio audio,
        LanguageId targetLanguageId,
        LanguageId? sourceLanguageId = null,
        RequestId? id = null)
        : base(id)
    {
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentNullException.ThrowIfNull(targetLanguageId);

        Audio = audio;
        TargetLanguageId = targetLanguageId;
        SourceLanguageId = sourceLanguageId;
    }
}
