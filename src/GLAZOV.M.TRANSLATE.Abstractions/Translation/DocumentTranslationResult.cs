using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;

namespace GLAZOV.M.TRANSLATE.Abstractions.Translation;

/// <summary>
/// Represents the immutable result of a single document translation operation.
/// </summary>
/// <remarks>
/// Instances of this class are immutable and thread-safe.
/// </remarks>
public sealed class DocumentTranslationResult : ProviderResult
{
    /// <summary>
    /// Gets the translated document. Its format is the one the provider
    /// produced, which is not always the format of the document sent: a
    /// provider may give a PDF back as a Word document.
    /// </summary>
    public ProviderDocument TranslatedDocument { get; }

    /// <summary>
    /// Gets the identifier of the language the document was translated from.
    /// </summary>
    public LanguageId SourceLanguageId { get; }

    /// <summary>
    /// Gets the identifier of the language the document was translated into.
    /// </summary>
    public LanguageId TargetLanguageId { get; }

    /// <summary>
    /// Gets a value indicating whether the source language was detected by the
    /// provider rather than named by the request.
    /// </summary>
    public bool WasSourceLanguageDetected { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentTranslationResult"/> class.
    /// </summary>
    /// <param name="requestId">
    /// The identifier of the request this result was produced from.
    /// </param>
    /// <param name="translatedDocument">
    /// The translated document.
    /// </param>
    /// <param name="sourceLanguageId">
    /// The identifier of the language the document was translated from.
    /// </param>
    /// <param name="targetLanguageId">
    /// The identifier of the language the document was translated into.
    /// </param>
    /// <param name="wasSourceLanguageDetected">
    /// <see langword="true"/> when <paramref name="sourceLanguageId"/> was
    /// detected by the provider; otherwise <see langword="false"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when one of the required arguments is <see langword="null"/>.
    /// </exception>
    public DocumentTranslationResult(
        RequestId requestId,
        ProviderDocument translatedDocument,
        LanguageId sourceLanguageId,
        LanguageId targetLanguageId,
        bool wasSourceLanguageDetected)
        : base(requestId)
    {
        ArgumentNullException.ThrowIfNull(translatedDocument);
        ArgumentNullException.ThrowIfNull(sourceLanguageId);
        ArgumentNullException.ThrowIfNull(targetLanguageId);

        TranslatedDocument = translatedDocument;
        SourceLanguageId = sourceLanguageId;
        TargetLanguageId = targetLanguageId;
        WasSourceLanguageDetected = wasSourceLanguageDetected;
    }
}
