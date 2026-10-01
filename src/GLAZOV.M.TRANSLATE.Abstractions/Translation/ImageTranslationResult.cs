using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using System.Collections.Immutable;

namespace GLAZOV.M.TRANSLATE.Abstractions.Translation;

/// <summary>
/// Represents the immutable result of a single image translation operation.
/// </summary>
/// <remarks>
/// Instances of this class are immutable and thread-safe.
/// </remarks>
public sealed class ImageTranslationResult : ProviderResult
{
    /// <summary>
    /// Gets the lines read from the image, each with its translation and with
    /// where it sits on the image. An image with no text on it yields none.
    /// </summary>
    public ImmutableArray<TranslatedLine> Lines { get; }

    /// <summary>
    /// Gets the identifier of the language the text was translated from.
    /// </summary>
    public LanguageId SourceLanguageId { get; }

    /// <summary>
    /// Gets the identifier of the language the text was translated into.
    /// </summary>
    public LanguageId TargetLanguageId { get; }

    /// <summary>
    /// Gets a value indicating whether the source language was detected by the
    /// provider rather than named by the request.
    /// </summary>
    public bool WasSourceLanguageDetected { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageTranslationResult"/> class.
    /// </summary>
    /// <param name="requestId">
    /// The identifier of the request this result was produced from.
    /// </param>
    /// <param name="lines">
    /// The lines read from the image, each with its translation.
    /// </param>
    /// <param name="sourceLanguageId">
    /// The identifier of the language the text was translated from.
    /// </param>
    /// <param name="targetLanguageId">
    /// The identifier of the language the text was translated into.
    /// </param>
    /// <param name="wasSourceLanguageDetected">
    /// <see langword="true"/> when <paramref name="sourceLanguageId"/> was
    /// detected by the provider; otherwise <see langword="false"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when one of the required arguments is <see langword="null"/>.
    /// </exception>
    public ImageTranslationResult(
        RequestId requestId,
        ImmutableArray<TranslatedLine> lines,
        LanguageId sourceLanguageId,
        LanguageId targetLanguageId,
        bool wasSourceLanguageDetected)
        : base(requestId)
    {
        ArgumentNullException.ThrowIfNull(sourceLanguageId);
        ArgumentNullException.ThrowIfNull(targetLanguageId);

        Lines = lines.IsDefault ? [] : lines;
        SourceLanguageId = sourceLanguageId;
        TargetLanguageId = targetLanguageId;
        WasSourceLanguageDetected = wasSourceLanguageDetected;
    }
}
