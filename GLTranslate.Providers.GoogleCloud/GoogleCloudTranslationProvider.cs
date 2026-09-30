using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;
using GLTranslate.Providers.GoogleCloud.Internal;

namespace GLTranslate.Providers.GoogleCloud;

/// <summary>
/// Translates text with the official Google Cloud Translation API.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <c>GoogleTranslationProvider</c>, which speaks to the undocumented
/// endpoint of the translate.google.com page and may stop working without
/// notice, this provider speaks to the billed, documented and supported API
/// (Cloud Translation, edition v2), with an API key.
/// </para>
/// <para>
/// The provider is thread-safe as long as the <see cref="HttpClient"/> it was
/// given is.
/// </para>
/// </remarks>
public sealed class GoogleCloudTranslationProvider : ITextTranslationProvider, IDisposable
{
    private readonly GoogleCloudEngine _engine;

    /// <inheritdoc/>
    public string Name => GoogleCloudProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleCloudTranslationProvider"/> class.
    /// </summary>
    /// <param name="credentials">
    /// The key the requests are made with.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="credentials"/> is <see langword="null"/>.
    /// </exception>
    public GoogleCloudTranslationProvider(GoogleCloudCredentials credentials)
    {
        _engine = new GoogleCloudEngine(credentials);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleCloudTranslationProvider"/>
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
    public GoogleCloudTranslationProvider(GoogleCloudCredentials credentials, HttpClient httpClient)
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
    public async Task<TextTranslationResult> ExecuteAsync(
        TextTranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string targetCode = GoogleCloudLanguageCodeResolver.ToGoogleCloudCode(request.TargetLanguageId);

        string? sourceCode = request.SourceLanguageId is null
            ? null
            : GoogleCloudLanguageCodeResolver.ToGoogleCloudCode(request.SourceLanguageId);

        (IReadOnlyList<string> translations, string? detectedSourceCode) = await _engine
            .TranslateAsync([request.Text.Value], isMarkup: false, sourceCode, targetCode, cancellationToken)
            .ConfigureAwait(false);

        return new TextTranslationResult(
            request.Id,
            new ProviderText(translations[0]),
            ResolveSource(request.SourceLanguageId, detectedSourceCode),
            request.TargetLanguageId,
            wasSourceLanguageDetected: request.SourceLanguageId is null);
    }

    private static LanguageId ResolveSource(LanguageId? named, string? detectedCode)
    {
        return named
            ?? (string.IsNullOrWhiteSpace(detectedCode)
                ? throw new ProviderException(
                    GoogleCloudProvider.Name,
                    "Google Cloud named no source language, although none was given.")
                : GoogleCloudLanguageCodeResolver.FromGoogleCloudCode(detectedCode));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
