using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;

namespace GLTranslate.Abstractions.Translation;

/// <summary>
/// Represents the immutable outcome of a single text translation
/// operation.
/// </summary>
/// <remarks>
/// Instances of this class are immutable and thread-safe.
/// </remarks>
public sealed class TextTranslationResult : ProviderResult
{
    /// <summary>
    /// Gets the translated text.
    /// </summary>
    public ProviderText TranslatedText { get; }

    /// <summary>
    /// Gets the identifier of the language the source text was written in.
    /// </summary>
    /// <remarks>
    /// This reflects the language actually used to produce the translation,
    /// whether it was supplied on the request or detected by the provider.
    /// </remarks>
    public LanguageId SourceLanguageId { get; }

    /// <summary>
    /// Gets the identifier of the language the text was translated into.
    /// </summary>
    public LanguageId TargetLanguageId { get; }

    /// <summary>
    /// Gets a value indicating whether <see cref="SourceLanguageId"/> was
    /// detected automatically rather than supplied on the request.
    /// </summary>
    public bool WasSourceLanguageDetected { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextTranslationResult"/> class.
    /// </summary>
    /// <param name="requestId">
    /// The identifier of the <see cref="TextTranslationRequest"/> this result
    /// was produced from.
    /// </param>
    /// <param name="translatedText">
    /// The translated text.
    /// </param>
    /// <param name="sourceLanguageId">
    /// The identifier of the language the source text was written in.
    /// </param>
    /// <param name="targetLanguageId">
    /// The identifier of the language the text was translated into.
    /// </param>
    /// <param name="wasSourceLanguageDetected">
    /// <see langword="true"/> when <paramref name="sourceLanguageId"/> was
    /// detected automatically; otherwise <see langword="false"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="requestId"/>, <paramref name="translatedText"/>,
    /// <paramref name="sourceLanguageId"/> or <paramref name="targetLanguageId"/>
    /// is <see langword="null"/>.
    /// </exception>
    public TextTranslationResult(
        RequestId requestId,
        ProviderText translatedText,
        LanguageId sourceLanguageId,
        LanguageId targetLanguageId,
        bool wasSourceLanguageDetected)
        : base(requestId)
    {
        ArgumentNullException.ThrowIfNull(translatedText);
        ArgumentNullException.ThrowIfNull(sourceLanguageId);
        ArgumentNullException.ThrowIfNull(targetLanguageId);

        TranslatedText = translatedText;
        SourceLanguageId = sourceLanguageId;
        TargetLanguageId = targetLanguageId;
        WasSourceLanguageDetected = wasSourceLanguageDetected;
    }
}
