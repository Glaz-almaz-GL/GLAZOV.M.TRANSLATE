using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Abstractions.Translation;

namespace GLAZOV.M.TRANSLATE.Providers.Common;

/// <summary>
/// Represents a provider that translates plain text, with everything around the
/// translation itself done: the languages put into the provider's terms, and
/// the result put together.
/// </summary>
/// <remarks>
/// A derived provider says only how its engine translates a text:
/// <see cref="TranslateAsync"/>.
/// </remarks>
public abstract class TextTranslationProviderBase : ITextTranslationProvider
{
    private readonly LanguageCodeResolver _languages;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextTranslationProviderBase"/> class.
    /// </summary>
    /// <param name="languages">
    /// How the languages of GLAZOV.M.TRANSLATE are written in the provider's terms.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when an argument is <see langword="null"/>.
    /// </exception>
    protected TextTranslationProviderBase(LanguageCodeResolver languages)
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
    /// Thrown when a language of the request is unknown to GLAZOV.M.TRANSLATE, when the
    /// provider fails or refuses, or when it names no source language although
    /// the request named none.
    /// </exception>
    public async Task<TextTranslationResult> ExecuteAsync(
        TextTranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string targetCode = _languages.ToProviderCode(request.TargetLanguageId);

        string? sourceCode = request.SourceLanguageId is null
            ? null
            : _languages.ToProviderCode(request.SourceLanguageId);

        ProviderTranslation translation = await TranslateAsync(
            request.Text.Value,
            sourceCode,
            targetCode,
            cancellationToken)
            .ConfigureAwait(false);

        return new TextTranslationResult(
            request.Id,
            new ProviderText(translation.Text),
            _languages.ResolveSource(request.SourceLanguageId, translation.DetectedSourceLanguageCode),
            request.TargetLanguageId,
            wasSourceLanguageDetected: request.SourceLanguageId is null);
    }

    /// <summary>
    /// Translates a text with the provider's engine.
    /// </summary>
    /// <param name="text">
    /// The text to translate.
    /// </param>
    /// <param name="sourceLanguageCode">
    /// The provider's code of the language the text is written in, or
    /// <see langword="null"/> to let the provider detect it.
    /// </param>
    /// <param name="targetLanguageCode">
    /// The provider's code of the language to translate into.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// The translation and, when the provider reports it, the code of the
    /// language it detected.
    /// </returns>
    protected abstract Task<ProviderTranslation> TranslateAsync(
        string text,
        string? sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken);
}
