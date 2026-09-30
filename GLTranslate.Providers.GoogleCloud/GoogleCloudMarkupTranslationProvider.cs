using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;
using GLTranslate.Providers.GoogleCloud.Internal;

namespace GLTranslate.Providers.GoogleCloud;

/// <summary>
/// Translates markup with the official Google Cloud Translation API, leaving
/// its tags where they are.
/// </summary>
/// <remarks>
/// <para>
/// The API translates the words between the tags of HTML and keeps the tags
/// themselves. It writes special characters of the translation as HTML
/// entities, as HTML does.
/// </para>
/// <para>
/// The provider is thread-safe as long as the <see cref="HttpClient"/> it was
/// given is.
/// </para>
/// </remarks>
public sealed class GoogleCloudMarkupTranslationProvider : IMarkupTranslationProvider, IDisposable
{
    private readonly GoogleCloudEngine _engine;

    /// <inheritdoc/>
    public string Name => GoogleCloudProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleCloudMarkupTranslationProvider"/> class.
    /// </summary>
    /// <param name="credentials">
    /// The key the requests are made with.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="credentials"/> is <see langword="null"/>.
    /// </exception>
    public GoogleCloudMarkupTranslationProvider(GoogleCloudCredentials credentials)
    {
        _engine = new GoogleCloudEngine(credentials);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleCloudMarkupTranslationProvider"/>
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
    public GoogleCloudMarkupTranslationProvider(GoogleCloudCredentials credentials, HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new GoogleCloudEngine(credentials, httpClient);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when a language of the request is unknown to GLTranslate, when
    /// Google refuses the request, or when the answer cannot be read.
    /// </exception>
    public async Task<MarkupTranslationResult> ExecuteAsync(
        MarkupTranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string targetCode = GoogleCloudLanguageCodeResolver.ToGoogleCloudCode(request.TargetLanguageId);

        string? sourceCode = request.SourceLanguageId is null
            ? null
            : GoogleCloudLanguageCodeResolver.ToGoogleCloudCode(request.SourceLanguageId);

        (IReadOnlyList<string> translations, string? detectedSourceCode) = await _engine
            .TranslateAsync([request.Markup.Value], isMarkup: true, sourceCode, targetCode, cancellationToken)
            .ConfigureAwait(false);

        LanguageId resolvedSourceLanguageId = request.SourceLanguageId
            ?? (string.IsNullOrWhiteSpace(detectedSourceCode)
                ? throw new ProviderException(
                    GoogleCloudProvider.Name,
                    "Google Cloud named no source language, although none was given.")
                : GoogleCloudLanguageCodeResolver.FromGoogleCloudCode(detectedSourceCode));

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
