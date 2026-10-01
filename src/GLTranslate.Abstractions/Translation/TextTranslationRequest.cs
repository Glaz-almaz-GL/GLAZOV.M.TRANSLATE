using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;

namespace GLTranslate.Abstractions.Translation;

/// <summary>
/// Represents the immutable parameters of a single text translation
/// operation.
/// </summary>
/// <remarks>
/// Instances of this class are immutable and thread-safe.
/// </remarks>
public sealed class TextTranslationRequest : ProviderRequest
{
    /// <summary>
    /// Gets the text to translate.
    /// </summary>
    public ProviderText Text { get; }

    /// <summary>
    /// Gets the identifier of the language to translate the text into.
    /// </summary>
    public LanguageId TargetLanguageId { get; }

    /// <summary>
    /// Gets the identifier of the language the text is written in.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/>, the provider is expected to detect the
    /// source language automatically.
    /// </remarks>
    public LanguageId? SourceLanguageId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextTranslationRequest"/> class.
    /// </summary>
    /// <param name="text">
    /// The text to translate.
    /// </param>
    /// <param name="targetLanguageId">
    /// The identifier of the language to translate the text into.
    /// </param>
    /// <param name="sourceLanguageId">
    /// The identifier of the language the text is written in, or
    /// <see langword="null"/> to request automatic language detection.
    /// </param>
    /// <param name="id">
    /// The request identifier, or <see langword="null"/> to generate a new one.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text"/> or <paramref name="targetLanguageId"/>
    /// is <see langword="null"/>.
    /// </exception>
    public TextTranslationRequest(
        ProviderText text,
        LanguageId targetLanguageId,
        LanguageId? sourceLanguageId = null,
        RequestId? id = null)
        : base(id)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(targetLanguageId);

        Text = text;
        TargetLanguageId = targetLanguageId;
        SourceLanguageId = sourceLanguageId;
    }
}
