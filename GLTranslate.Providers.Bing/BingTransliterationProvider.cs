using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Transliteration;
using GLTranslate.Providers.Bing.Internal;

namespace GLTranslate.Providers.Bing;

/// <summary>
/// Renders text in the Latin script with Bing Translator.
/// </summary>
/// <remarks>
/// <para>
/// Bing has no endpoint of its own for this: it romanizes the source text as
/// a by-product of translating it, so the provider asks for a translation
/// into English and keeps only that rendering. The endpoint detects the
/// language itself, so a request need not name it.
/// </para>
/// <para>
/// A text already written in the Latin script is not romanized, and the
/// endpoint returns nothing to keep; such a request is refused with a message
/// that says so.
/// </para>
/// <para>
/// The provider is thread-safe as long as the <see cref="HttpClient"/> it was
/// given is.
/// </para>
/// </remarks>
public sealed class BingTransliterationProvider : ITransliterationProvider, IDisposable
{
    // The language to translate into while asking for the rendering. It only
    // has to be a language Bing translates into; the translation is discarded.
    private const string TargetLanguageCode = "en";

    private readonly BingEngine _engine;

    /// <inheritdoc/>
    public string Name => BingProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="BingTransliterationProvider"/> class.
    /// </summary>
    public BingTransliterationProvider()
    {
        _engine = new BingEngine();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BingTransliterationProvider"/>
    /// class with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient"/> is <see langword="null"/>.
    /// </exception>
    public BingTransliterationProvider(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new BingEngine(httpClient);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the language is unknown to GLTranslate, when the text is
    /// already written in the Latin script, when Bing refuses the request, or
    /// when the endpoint answers with something the provider cannot read.
    /// </exception>
    public async Task<TransliterationResult> ExecuteAsync(
        TransliterationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string? sourceCode = request.LanguageId is null
            ? null
            : BingLanguageCodeResolver.ToBingCode(request.LanguageId);

        BingAnswer answer = await _engine
            .TranslateAsync(request.Text.Value, TargetLanguageCode, sourceCode, cancellationToken)
            .ConfigureAwait(false);

        if (answer.InputTransliteration is not { } transliteratedText)
        {
            throw new ProviderException(
                BingProvider.Name,
                "Bing Translator rendered nothing: the text is already written in the Latin script.");
        }

        LanguageId languageId = request.LanguageId
            ?? BingLanguageCodeResolver.FromBingCode(answer.SourceLanguageCode);

        return new TransliterationResult(
            request.Id,
            new TransliteratedText(transliteratedText),
            languageId,
            wasLanguageDetected: request.LanguageId is null);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
