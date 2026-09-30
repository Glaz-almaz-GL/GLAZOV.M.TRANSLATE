using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;

namespace GLTranslate.Providers.Common;

/// <summary>
/// Represents a provider that reads the text on an image and translates it,
/// with everything around that done: the languages put into the provider's
/// terms, and the result put together.
/// </summary>
/// <remarks>
/// <para>
/// A derived provider says only how its engine reads and translates an image:
/// <see cref="TranslateAsync"/>.
/// </para>
/// <para>
/// An image with no text on it is not a failure: there is simply nothing to
/// translate. The result has no lines, and its source language is the one the
/// request named, or the one the provider found, or, when the provider found
/// none, the language asked for, which is the only one there is to report.
/// </para>
/// </remarks>
public abstract class ImageTranslationProviderBase : IImageTranslationProvider
{
    private readonly LanguageCodeResolver _languages;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageTranslationProviderBase"/> class.
    /// </summary>
    /// <param name="languages">
    /// How the languages of GLTranslate are written in the provider's terms.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when an argument is <see langword="null"/>.
    /// </exception>
    protected ImageTranslationProviderBase(LanguageCodeResolver languages)
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
    /// Thrown when a language of the request is unknown to GLTranslate, or when
    /// the provider fails, refuses, or cannot read the image.
    /// </exception>
    public async Task<ImageTranslationResult> ExecuteAsync(
        ImageTranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string targetCode = _languages.ToProviderCode(request.TargetLanguageId);

        string? sourceCode = request.SourceLanguageId is null
            ? null
            : _languages.ToProviderCode(request.SourceLanguageId);

        ImageTranslation translation = await TranslateAsync(
            request.Image,
            sourceCode,
            targetCode,
            cancellationToken)
            .ConfigureAwait(false);

        // The provider may name no language even when it found text, or find
        // none because there was none: there is then nothing better to report
        // than the language that was asked for.
        LanguageId sourceLanguageId = request.SourceLanguageId
            ?? (string.IsNullOrWhiteSpace(translation.SourceLanguageCode)
                ? request.TargetLanguageId
                : _languages.FromProviderCode(translation.SourceLanguageCode));

        return new ImageTranslationResult(
            request.Id,
            translation.Lines,
            sourceLanguageId,
            request.TargetLanguageId,
            wasSourceLanguageDetected: request.SourceLanguageId is null);
    }

    /// <summary>
    /// Reads the text on an image and translates it with the provider's engine.
    /// </summary>
    /// <param name="image">
    /// The image to read.
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
    /// The lines read and translated and, when the provider reports it, the
    /// code of the language of the text.
    /// </returns>
    protected abstract Task<ImageTranslation> TranslateAsync(
        ProviderImage image,
        string? sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken);
}
