using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;

namespace GLTranslate.Providers.Common;

/// <summary>
/// Represents a provider that translates markup, leaving its tags where they
/// are, with everything around the translation itself done: the languages put
/// into the provider's terms, and the result put together.
/// </summary>
/// <remarks>
/// A derived provider says only how its engine translates markup:
/// <see cref="TranslateAsync"/>.
/// </remarks>
public abstract class MarkupTranslationProviderBase : IMarkupTranslationProvider
{
    private readonly LanguageCodeResolver _languages;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkupTranslationProviderBase"/> class.
    /// </summary>
    /// <param name="languages">
    /// How the languages of GLTranslate are written in the provider's terms.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when an argument is <see langword="null"/>.
    /// </exception>
    protected MarkupTranslationProviderBase(LanguageCodeResolver languages)
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
    /// Thrown when a language of the request is unknown to GLTranslate, when the
    /// provider fails or refuses, or when it names no source language although
    /// the request named none.
    /// </exception>
    public async Task<MarkupTranslationResult> ExecuteAsync(
        MarkupTranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string targetCode = _languages.ToProviderCode(request.TargetLanguageId);

        string? sourceCode = request.SourceLanguageId is null
            ? null
            : _languages.ToProviderCode(request.SourceLanguageId);

        ProviderTranslation translation = await TranslateAsync(
            request.Markup.Value,
            sourceCode,
            targetCode,
            cancellationToken)
            .ConfigureAwait(false);

        return new MarkupTranslationResult(
            request.Id,
            new ProviderMarkup(translation.Text),
            _languages.ResolveSource(request.SourceLanguageId, translation.DetectedSourceLanguageCode),
            request.TargetLanguageId,
            wasSourceLanguageDetected: request.SourceLanguageId is null);
    }

    /// <summary>
    /// Translates markup with the provider's engine.
    /// </summary>
    /// <param name="markup">
    /// The markup to translate.
    /// </param>
    /// <param name="sourceLanguageCode">
    /// The provider's code of the language the markup is written in, or
    /// <see langword="null"/> to let the provider detect it.
    /// </param>
    /// <param name="targetLanguageCode">
    /// The provider's code of the language to translate into.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// The translated markup and, when the provider reports it, the code of the
    /// language it detected.
    /// </returns>
    protected abstract Task<ProviderTranslation> TranslateAsync(
        string markup,
        string? sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken);
}
