using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;

namespace GLAZOV.M.TRANSLATE.Abstractions.Translation;

/// <summary>
/// Represents the immutable parameters of a single document translation
/// operation.
/// </summary>
/// <remarks>
/// Instances of this class are immutable and thread-safe.
/// </remarks>
public sealed class DocumentTranslationRequest : ProviderRequest
{
    /// <summary>
    /// Gets the document to translate.
    /// </summary>
    public ProviderDocument Document { get; }

    /// <summary>
    /// Gets the identifier of the language to translate into.
    /// </summary>
    public LanguageId TargetLanguageId { get; }

    /// <summary>
    /// Gets the identifier of the language the document is written in, or
    /// <see langword="null"/> to let the provider detect it.
    /// </summary>
    public LanguageId? SourceLanguageId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentTranslationRequest"/> class.
    /// </summary>
    /// <param name="document">
    /// The document to translate.
    /// </param>
    /// <param name="targetLanguageId">
    /// The identifier of the language to translate into.
    /// </param>
    /// <param name="sourceLanguageId">
    /// The identifier of the language the document is written in, or
    /// <see langword="null"/> to let the provider detect it.
    /// </param>
    /// <param name="id">
    /// The request identifier, or <see langword="null"/> to generate a new one.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="document"/> or <paramref name="targetLanguageId"/>
    /// is <see langword="null"/>.
    /// </exception>
    public DocumentTranslationRequest(
        ProviderDocument document,
        LanguageId targetLanguageId,
        LanguageId? sourceLanguageId = null,
        RequestId? id = null)
        : base(id)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(targetLanguageId);

        Document = document;
        TargetLanguageId = targetLanguageId;
        SourceLanguageId = sourceLanguageId;
    }
}
