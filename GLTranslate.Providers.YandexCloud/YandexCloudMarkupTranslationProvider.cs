using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;
using GLTranslate.Providers.YandexCloud.Internal;

namespace GLTranslate.Providers.YandexCloud;

/// <summary>
/// Translates markup with the official Yandex Cloud Translate API, leaving its
/// tags where they are.
/// </summary>
/// <remarks>
/// <para>
/// The API translates the words between the tags of HTML and keeps the tags
/// themselves. Yandex takes 10,000 characters in a request.
/// </para>
/// <para>
/// The provider is thread-safe as long as the <see cref="HttpClient"/> it was
/// given is.
/// </para>
/// </remarks>
public sealed class YandexCloudMarkupTranslationProvider : IMarkupTranslationProvider, IDisposable
{
    private readonly YandexCloudEngine _engine;

    /// <inheritdoc/>
    public string Name => YandexCloudProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="YandexCloudMarkupTranslationProvider"/> class.
    /// </summary>
    /// <param name="credentials">
    /// The key the requests are made with.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="credentials"/> is <see langword="null"/>.
    /// </exception>
    public YandexCloudMarkupTranslationProvider(YandexCloudCredentials credentials)
    {
        _engine = new YandexCloudEngine(credentials);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YandexCloudMarkupTranslationProvider"/>
    /// class with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="credentials">
    /// The key the requests are made with.
    /// </param>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when an argument is <see langword="null"/>.
    /// </exception>
    public YandexCloudMarkupTranslationProvider(YandexCloudCredentials credentials, HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new YandexCloudEngine(credentials, httpClient);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when a language of the request is unknown to GLTranslate, when
    /// the markup is too long, when Yandex refuses the request, or when the
    /// answer cannot be read.
    /// </exception>
    public async Task<MarkupTranslationResult> ExecuteAsync(
        MarkupTranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string targetCode = YandexCloudLanguageCodeResolver.ToYandexCloudCode(request.TargetLanguageId);

        string? sourceCode = request.SourceLanguageId is null
            ? null
            : YandexCloudLanguageCodeResolver.ToYandexCloudCode(request.SourceLanguageId);

        (IReadOnlyList<string> translations, string? detectedSourceCode) = await _engine
            .TranslateAsync([request.Markup.Value], isMarkup: true, sourceCode, targetCode, cancellationToken)
            .ConfigureAwait(false);

        LanguageId resolvedSourceLanguageId = request.SourceLanguageId
            ?? (string.IsNullOrWhiteSpace(detectedSourceCode)
                ? throw new ProviderException(
                    YandexCloudProvider.Name,
                    "Yandex Cloud named no source language, although none was given.")
                : YandexCloudLanguageCodeResolver.FromYandexCloudCode(detectedSourceCode));

        return new MarkupTranslationResult(
            request.Id,
            new ProviderMarkup(translations[0]),
            resolvedSourceLanguageId,
            request.TargetLanguageId,
            wasSourceLanguageDetected: request.SourceLanguageId is null);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
