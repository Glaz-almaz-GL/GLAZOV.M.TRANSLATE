using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;
using GLTranslate.Providers.Microsoft.Internal;

namespace GLTranslate.Providers.Microsoft;

/// <summary>
/// Translates text with Microsoft Translator.
/// </summary>
/// <remarks>
/// <para>
/// The provider needs no subscription key: every request is signed the way
/// the Microsoft Translator mobile application signs its own.
/// </para>
/// <para>
/// The provider is thread-safe as long as the <see cref="HttpClient"/> it was
/// given is.
/// </para>
/// </remarks>
public sealed class MicrosoftTranslationProvider : ITextTranslationProvider, IDisposable
{
    private readonly MicrosoftTranslationEngine _engine;

    /// <inheritdoc/>
    public string Name => MicrosoftProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrosoftTranslationProvider"/> class.
    /// </summary>
    public MicrosoftTranslationProvider()
    {
        _engine = new MicrosoftTranslationEngine();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrosoftTranslationProvider"/>
    /// class with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient"/> is <see langword="null"/>.
    /// </exception>
    public MicrosoftTranslationProvider(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new MicrosoftTranslationEngine(httpClient);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when a language of the request is unknown to Microsoft
    /// Translator, when the request fails, or when the endpoint answers with
    /// something the provider cannot read.
    /// </exception>
    public async Task<TextTranslationResult> ExecuteAsync(
        TextTranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string targetCode = MicrosoftLanguageCodeResolver.ToMicrosoftCode(request.TargetLanguageId);

        string? sourceCode = request.SourceLanguageId is null
            ? null
            : MicrosoftLanguageCodeResolver.ToMicrosoftCode(request.SourceLanguageId);

        (string translatedText, string detectedSourceCode) = await _engine
            .TranslateAsync(request.Text.Value, targetCode, sourceCode, cancellationToken)
            .ConfigureAwait(false);

        LanguageId resolvedSourceLanguageId = request.SourceLanguageId
            ?? MicrosoftLanguageCodeResolver.FromMicrosoftCode(detectedSourceCode);

        return new TextTranslationResult(
            request.Id,
            new ProviderText(translatedText),
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
