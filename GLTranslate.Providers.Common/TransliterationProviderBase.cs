using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Transliteration;

namespace GLTranslate.Providers.Common;

/// <summary>
/// Represents a provider that writes a text in the Latin script, with everything
/// around the transliteration itself done: the language put into the provider's
/// terms, and the result put together.
/// </summary>
/// <remarks>
/// A derived provider says only how its engine transliterates a text:
/// <see cref="TransliterateAsync"/>.
/// </remarks>
public abstract class TransliterationProviderBase : ITransliterationProvider
{
    private readonly LanguageCodeResolver _languages;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransliterationProviderBase"/> class.
    /// </summary>
    /// <param name="languages">
    /// How the languages of GLTranslate are written in the provider's terms.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when an argument is <see langword="null"/>.
    /// </exception>
    protected TransliterationProviderBase(LanguageCodeResolver languages)
    {
        ArgumentNullException.ThrowIfNull(languages);

        _languages = languages;
    }

    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the language of the request is unknown to GLTranslate, when
    /// the provider fails or refuses, or when it names no language although the
    /// request named none.
    /// </exception>
    public async Task<TransliterationResult> ExecuteAsync(
        TransliterationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string? languageCode = request.LanguageId is null
            ? null
            : _languages.ToProviderCode(request.LanguageId);

        ProviderTransliteration transliteration = await TransliterateAsync(
            request.Text.Value,
            request.LanguageId,
            languageCode,
            cancellationToken)
            .ConfigureAwait(false);

        return new TransliterationResult(
            request.Id,
            new TransliteratedText(transliteration.Text),
            _languages.ResolveSource(request.LanguageId, transliteration.DetectedLanguageCode),
            wasLanguageDetected: request.LanguageId is null);
    }

    /// <summary>
    /// Transliterates a text with the provider's engine.
    /// </summary>
    /// <param name="text">
    /// The text to transliterate.
    /// </param>
    /// <param name="languageId">
    /// The language the text is written in, or <see langword="null"/> to let the
    /// provider detect it.
    /// </param>
    /// <param name="languageCode">
    /// The provider's code of <paramref name="languageId"/>, or
    /// <see langword="null"/> when there is no language.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// The transliteration and, when the provider reports it, the code of the
    /// language it detected.
    /// </returns>
    protected abstract Task<ProviderTransliteration> TransliterateAsync(
        string text,
        LanguageId? languageId,
        string? languageCode,
        CancellationToken cancellationToken);
}
