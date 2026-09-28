using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;

namespace GLTranslate.Abstractions.Translation;

/// <summary>
/// Represents the immutable result of a single markup translation operation.
/// </summary>
/// <remarks>
/// Instances of this class are immutable and thread-safe.
/// </remarks>
public sealed class MarkupTranslationResult : ProviderResult
{
    /// <summary>
    /// Gets the translated markup, carrying the tags of the requested markup.
    /// </summary>
    public ProviderMarkup TranslatedMarkup { get; }

    /// <summary>
    /// Gets the identifier of the language the markup was translated from.
    /// </summary>
    public LanguageId SourceLanguageId { get; }

    /// <summary>
    /// Gets the identifier of the language the markup was translated into.
    /// </summary>
    public LanguageId TargetLanguageId { get; }

    /// <summary>
    /// Gets a value indicating whether the source language was detected by the
    /// provider rather than named by the request.
    /// </summary>
    public bool WasSourceLanguageDetected { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkupTranslationResult"/> class.
    /// </summary>
    /// <param name="requestId">
    /// The identifier of the request this result was produced from.
    /// </param>
    /// <param name="translatedMarkup">
    /// The translated markup.
    /// </param>
    /// <param name="sourceLanguageId">
    /// The identifier of the language the markup was translated from.
    /// </param>
    /// <param name="targetLanguageId">
    /// The identifier of the language the markup was translated into.
    /// </param>
    /// <param name="wasSourceLanguageDetected">
    /// <see langword="true"/> when <paramref name="sourceLanguageId"/> was
    /// detected by the provider; otherwise <see langword="false"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when one of the required arguments is <see langword="null"/>.
    /// </exception>
    public MarkupTranslationResult(
        RequestId requestId,
        ProviderMarkup translatedMarkup,
        LanguageId sourceLanguageId,
        LanguageId targetLanguageId,
        bool wasSourceLanguageDetected)
        : base(requestId)
    {
        ArgumentNullException.ThrowIfNull(translatedMarkup);
        ArgumentNullException.ThrowIfNull(sourceLanguageId);
        ArgumentNullException.ThrowIfNull(targetLanguageId);

        TranslatedMarkup = translatedMarkup;
        SourceLanguageId = sourceLanguageId;
        TargetLanguageId = targetLanguageId;
        WasSourceLanguageDetected = wasSourceLanguageDetected;
    }
}
