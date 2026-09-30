using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;

namespace GLTranslate.Abstractions.Translation;

/// <summary>
/// Represents the immutable result of a single audio translation operation.
/// </summary>
/// <remarks>
/// Instances of this class are immutable and thread-safe.
/// </remarks>
public sealed class AudioTranslationResult : ProviderResult
{
    /// <summary>
    /// Gets what was heard in the recording, in the language it was spoken in.
    /// </summary>
    public ProviderText RecognizedText { get; }

    /// <summary>
    /// Gets what was heard, translated.
    /// </summary>
    public ProviderText TranslatedText { get; }

    /// <summary>
    /// Gets the translation spoken aloud, or <see langword="null"/> when the
    /// provider gave none.
    /// </summary>
    public SpokenTranslation? Speech { get; }

    /// <summary>
    /// Gets the identifier of the language spoken in the recording.
    /// </summary>
    public LanguageId SourceLanguageId { get; }

    /// <summary>
    /// Gets the identifier of the language the speech was translated into.
    /// </summary>
    public LanguageId TargetLanguageId { get; }

    /// <summary>
    /// Gets a value indicating whether the source language was detected by the
    /// provider rather than named by the request.
    /// </summary>
    public bool WasSourceLanguageDetected { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioTranslationResult"/> class.
    /// </summary>
    /// <param name="requestId">
    /// The identifier of the request this result was produced from.
    /// </param>
    /// <param name="recognizedText">
    /// What was heard in the recording.
    /// </param>
    /// <param name="translatedText">
    /// What was heard, translated.
    /// </param>
    /// <param name="speech">
    /// The translation spoken aloud, or <see langword="null"/> when there is none.
    /// </param>
    /// <param name="sourceLanguageId">
    /// The identifier of the language spoken in the recording.
    /// </param>
    /// <param name="targetLanguageId">
    /// The identifier of the language the speech was translated into.
    /// </param>
    /// <param name="wasSourceLanguageDetected">
    /// <see langword="true"/> when <paramref name="sourceLanguageId"/> was
    /// detected by the provider; otherwise <see langword="false"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when one of the required arguments is <see langword="null"/>.
    /// </exception>
    public AudioTranslationResult(
        RequestId requestId,
        ProviderText recognizedText,
        ProviderText translatedText,
        SpokenTranslation? speech,
        LanguageId sourceLanguageId,
        LanguageId targetLanguageId,
        bool wasSourceLanguageDetected)
        : base(requestId)
    {
        ArgumentNullException.ThrowIfNull(recognizedText);
        ArgumentNullException.ThrowIfNull(translatedText);
        ArgumentNullException.ThrowIfNull(sourceLanguageId);
        ArgumentNullException.ThrowIfNull(targetLanguageId);

        RecognizedText = recognizedText;
        TranslatedText = translatedText;
        Speech = speech;
        SourceLanguageId = sourceLanguageId;
        TargetLanguageId = targetLanguageId;
        WasSourceLanguageDetected = wasSourceLanguageDetected;
    }
}
