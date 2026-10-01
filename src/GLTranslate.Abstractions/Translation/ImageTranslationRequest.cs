using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;

namespace GLTranslate.Abstractions.Translation;

/// <summary>
/// Represents the immutable parameters of a single image translation
/// operation.
/// </summary>
/// <remarks>
/// Instances of this class are immutable and thread-safe.
/// </remarks>
public sealed class ImageTranslationRequest : ProviderRequest
{
    /// <summary>
    /// Gets the image to read the text from.
    /// </summary>
    public ProviderImage Image { get; }

    /// <summary>
    /// Gets the identifier of the language to translate into.
    /// </summary>
    public LanguageId TargetLanguageId { get; }

    /// <summary>
    /// Gets the identifier of the language the text on the image is written
    /// in, or <see langword="null"/> to let the provider detect it.
    /// </summary>
    public LanguageId? SourceLanguageId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageTranslationRequest"/> class.
    /// </summary>
    /// <param name="image">
    /// The image to read the text from.
    /// </param>
    /// <param name="targetLanguageId">
    /// The identifier of the language to translate into.
    /// </param>
    /// <param name="sourceLanguageId">
    /// The identifier of the language the text on the image is written in, or
    /// <see langword="null"/> to let the provider detect it.
    /// </param>
    /// <param name="id">
    /// The request identifier, or <see langword="null"/> to generate a new one.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="image"/> or <paramref name="targetLanguageId"/>
    /// is <see langword="null"/>.
    /// </exception>
    public ImageTranslationRequest(
        ProviderImage image,
        LanguageId targetLanguageId,
        LanguageId? sourceLanguageId = null,
        RequestId? id = null)
        : base(id)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(targetLanguageId);

        Image = image;
        TargetLanguageId = targetLanguageId;
        SourceLanguageId = sourceLanguageId;
    }
}
