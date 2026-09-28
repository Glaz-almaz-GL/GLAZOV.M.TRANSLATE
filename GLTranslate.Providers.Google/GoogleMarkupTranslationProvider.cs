using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;
using GLTranslate.Providers.Google.Internal;

namespace GLTranslate.Providers.Google;

/// <summary>
/// Translates markup with Google Translate, leaving its tags where they are.
/// </summary>
/// <remarks>
/// <para>
/// The endpoint translates what stands between the tags and returns the
/// markup otherwise unchanged, so a page fragment comes back as the same
/// fragment in another language. Fetching the page is the caller's business:
/// this provider is handed markup and hands markup back.
/// </para>
/// <para>
/// The provider is thread-safe as long as the <see cref="HttpClient"/> it was
/// given is.
/// </para>
/// </remarks>
public sealed class GoogleMarkupTranslationProvider : IMarkupTranslationProvider, IDisposable
{
    private readonly GoogleMarkupTranslationEngine _engine;

    /// <inheritdoc/>
    public string Name => GoogleProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleMarkupTranslationProvider"/> class.
    /// </summary>
    public GoogleMarkupTranslationProvider()
    {
        _engine = new GoogleMarkupTranslationEngine();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleMarkupTranslationProvider"/>
    /// class with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient"/> is <see langword="null"/>.
    /// </exception>
    public GoogleMarkupTranslationProvider(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new GoogleMarkupTranslationEngine(httpClient);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when a language of the request is unknown to GLTranslate or one
    /// Google Translate cannot translate, when the request fails, or when the
    /// endpoint answers with something the provider cannot read.
    /// </exception>
    public async Task<MarkupTranslationResult> ExecuteAsync(
        MarkupTranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string sourceCode = GoogleLanguageCodeResolver.ToGoogleCode(request.SourceLanguageId);
        string targetCode = GoogleLanguageCodeResolver.ToGoogleCode(request.TargetLanguageId);

        (string translatedMarkup, string? detectedSourceCode) = await _engine
            .TranslateAsync(request.Markup.Value, sourceCode, targetCode, cancellationToken)
            .ConfigureAwait(false);

        LanguageId resolvedSourceLanguageId = request.SourceLanguageId
            ?? (detectedSourceCode is null
                ? throw new ProviderException(
                    GoogleProvider.Name,
                    "Google Translate detected no source language, although none was given.")
                : GoogleLanguageCodeResolver.FromGoogleCode(detectedSourceCode));

        return new MarkupTranslationResult(
            request.Id,
            new ProviderMarkup(translatedMarkup),
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
