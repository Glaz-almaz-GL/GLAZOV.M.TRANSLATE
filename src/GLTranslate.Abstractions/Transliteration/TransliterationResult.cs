using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;

namespace GLTranslate.Abstractions.Transliteration;

/// <summary>
/// Represents the immutable outcome of a single text transliteration
/// operation.
/// </summary>
/// <remarks>
/// Instances of this class are immutable and thread-safe.
/// </remarks>
public sealed class TransliterationResult : ProviderResult
{
    /// <summary>
    /// Gets the transliteration of the source text.
    /// </summary>
    public TransliteratedText Transliteration { get; }

    /// <summary>
    /// Gets the identifier of the language the source text was written in.
    /// </summary>
    /// <remarks>
    /// This reflects the language actually used to produce the
    /// transliteration, whether it was supplied on the request or detected
    /// by the provider.
    /// </remarks>
    public LanguageId LanguageId { get; }

    /// <summary>
    /// Gets a value indicating whether <see cref="LanguageId"/> was
    /// detected automatically rather than supplied on the request.
    /// </summary>
    public bool WasLanguageDetected { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TransliterationResult"/> class.
    /// </summary>
    /// <param name="requestId">
    /// The identifier of the <see cref="TransliterationRequest"/> this
    /// result was produced from.
    /// </param>
    /// <param name="transliteration">
    /// The transliteration of the source text.
    /// </param>
    /// <param name="languageId">
    /// The identifier of the language the source text was written in.
    /// </param>
    /// <param name="wasLanguageDetected">
    /// <see langword="true"/> when <paramref name="languageId"/> was
    /// detected automatically; otherwise <see langword="false"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="requestId"/>, <paramref name="transliteration"/>
    /// or <paramref name="languageId"/> is <see langword="null"/>.
    /// </exception>
    public TransliterationResult(
        RequestId requestId,
        TransliteratedText transliteration,
        LanguageId languageId,
        bool wasLanguageDetected)
        : base(requestId)
    {
        ArgumentNullException.ThrowIfNull(transliteration);
        ArgumentNullException.ThrowIfNull(languageId);

        Transliteration = transliteration;
        LanguageId = languageId;
        WasLanguageDetected = wasLanguageDetected;
    }
}
