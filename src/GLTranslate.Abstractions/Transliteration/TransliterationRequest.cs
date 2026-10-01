using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;

namespace GLTranslate.Abstractions.Transliteration;

/// <summary>
/// Represents the immutable parameters of a single text transliteration
/// operation.
/// </summary>
/// <remarks>
/// <para>
/// Unlike a text translation request, this request only concerns a single
/// language: the one the input text is written in. Transliteration renders
/// that same text phonetically; it does not translate it into another
/// language.
/// </para>
/// <para>
/// Instances of this class are immutable and thread-safe.
/// </para>
/// </remarks>
public sealed class TransliterationRequest : ProviderRequest
{
    /// <summary>
    /// Gets the text to transliterate.
    /// </summary>
    public ProviderText Text { get; }

    /// <summary>
    /// Gets the identifier of the language the text is written in.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/>, the provider is expected to detect the
    /// language automatically.
    /// </remarks>
    public LanguageId? LanguageId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TransliterationRequest"/> class.
    /// </summary>
    /// <param name="text">
    /// The text to transliterate.
    /// </param>
    /// <param name="languageId">
    /// The identifier of the language the text is written in, or
    /// <see langword="null"/> to request automatic language detection.
    /// </param>
    /// <param name="id">
    /// The request identifier, or <see langword="null"/> to generate a new one.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text"/> is <see langword="null"/>.
    /// </exception>
    public TransliterationRequest(ProviderText text, LanguageId? languageId = null, RequestId? id = null)
        : base(id)
    {
        ArgumentNullException.ThrowIfNull(text);

        Text = text;
        LanguageId = languageId;
    }
}
