using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;

namespace GLAZOV.M.TRANSLATE.Abstractions.Translation;

/// <summary>
/// Represents the immutable parameters of a single markup translation
/// operation.
/// </summary>
/// <remarks>
/// Instances of this class are immutable and thread-safe.
/// </remarks>
public sealed class MarkupTranslationRequest : ProviderRequest
{
    /// <summary>
    /// Gets the markup to translate.
    /// </summary>
    public ProviderMarkup Markup { get; }

    /// <summary>
    /// Gets the identifier of the language to translate into.
    /// </summary>
    public LanguageId TargetLanguageId { get; }

    /// <summary>
    /// Gets the identifier of the language the markup is written in, or
    /// <see langword="null"/> to let the provider detect it.
    /// </summary>
    public LanguageId? SourceLanguageId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkupTranslationRequest"/> class.
    /// </summary>
    /// <param name="markup">
    /// The markup to translate.
    /// </param>
    /// <param name="targetLanguageId">
    /// The identifier of the language to translate into.
    /// </param>
    /// <param name="sourceLanguageId">
    /// The identifier of the language the markup is written in, or
    /// <see langword="null"/> to let the provider detect it.
    /// </param>
    /// <param name="id">
    /// The request identifier, or <see langword="null"/> to generate a new one.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="markup"/> or <paramref name="targetLanguageId"/>
    /// is <see langword="null"/>.
    /// </exception>
    public MarkupTranslationRequest(
        ProviderMarkup markup,
        LanguageId targetLanguageId,
        LanguageId? sourceLanguageId = null,
        RequestId? id = null)
        : base(id)
    {
        ArgumentNullException.ThrowIfNull(markup);
        ArgumentNullException.ThrowIfNull(targetLanguageId);

        Markup = markup;
        TargetLanguageId = targetLanguageId;
        SourceLanguageId = sourceLanguageId;
    }
}
