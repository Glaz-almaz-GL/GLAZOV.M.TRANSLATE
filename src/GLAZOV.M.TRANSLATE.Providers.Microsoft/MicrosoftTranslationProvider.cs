using GLAZOV.M.TRANSLATE.Abstractions.Translation;
using GLAZOV.M.TRANSLATE.Providers.Microsoft.Internal;
using GLAZOV.M.TRANSLATE.Providers.Common;

namespace GLAZOV.M.TRANSLATE.Providers.Microsoft;

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
public sealed class MicrosoftTranslationProvider : TextTranslationProviderBase, IDisposable
{
    private readonly MicrosoftTranslationEngine _engine;

    /// <inheritdoc/>
    public override string Name => MicrosoftProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrosoftTranslationProvider"/> class.
    /// </summary>
    public MicrosoftTranslationProvider()
        : base(MicrosoftLanguageCodeResolver.Instance)
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
        : base(MicrosoftLanguageCodeResolver.Instance)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new MicrosoftTranslationEngine(httpClient);
    }

    /// <inheritdoc/>
    protected override async Task<ProviderTranslation> TranslateAsync(
        string text,
        string? sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken)
    {
        (string translatedText, string detectedSourceCode) = await _engine
            .TranslateAsync(text, targetLanguageCode, sourceLanguageCode, cancellationToken)
            .ConfigureAwait(false);

        return new ProviderTranslation(translatedText, detectedSourceCode);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
