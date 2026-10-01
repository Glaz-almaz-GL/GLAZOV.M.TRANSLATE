using GLAZOV.M.TRANSLATE.Providers.Common;
using GLAZOV.M.TRANSLATE.Providers.Papago.Internal;
using GLAZOV.M.TRANSLATE.Abstractions.Translation;

namespace GLAZOV.M.TRANSLATE.Providers.Papago;

/// <summary>
/// Translates text using the free web service of Naver Papago.
/// </summary>
/// <remarks>
/// <para>
/// It requires no API key. The service is undocumented, unsupported by its
/// owner, and has changed its calls before; the address is a setting of
/// <see cref="PapagoOptions"/> for that reason, and the provider may stop
/// working without notice.
/// </para>
/// <para>
/// Papago reads the Korean, Japanese, Chinese and most European languages well;
/// a pair it does not offer is refused by the service and reported as a
/// <see cref="Abstractions.Providers.ProviderException"/>.
/// </para>
/// <para>
/// Instances are immutable and thread-safe, provided the supplied
/// <see cref="HttpClient"/> is not mutated after construction.
/// </para>
/// </remarks>
public sealed class PapagoTranslationProvider : TextTranslationProviderBase, IDisposable
{
    private readonly PapagoEngine _engine;

    /// <inheritdoc/>
    public override string Name => PapagoProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="PapagoTranslationProvider"/>
    /// class with the default settings.
    /// </summary>
    public PapagoTranslationProvider()
        : this(new PapagoOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PapagoTranslationProvider"/>
    /// class with the given settings.
    /// </summary>
    /// <param name="options">The settings of the provider.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    public PapagoTranslationProvider(PapagoOptions options)
        : base(PapagoLanguageCodeResolver.Instance)
    {
        ArgumentNullException.ThrowIfNull(options);

        _engine = new PapagoEngine(options);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PapagoTranslationProvider"/>
    /// class that sends its requests through the given client.
    /// </summary>
    /// <param name="options">The settings of the provider.</param>
    /// <param name="httpClient">
    /// The HTTP client used to send requests. The provider does not own its
    /// lifetime.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> or <paramref name="httpClient"/>
    /// is <see langword="null"/>.
    /// </exception>
    public PapagoTranslationProvider(PapagoOptions options, HttpClient httpClient)
        : base(PapagoLanguageCodeResolver.Instance)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new PapagoEngine(options, httpClient);
    }

    /// <inheritdoc/>
    protected override async Task<ProviderTranslation> TranslateAsync(
        string text,
        string? sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken)
    {
        PapagoAnswer answer = await _engine
            .TranslateAsync(text, sourceLanguageCode ?? PapagoLanguageCodeResolver.AutoDetectCode, targetLanguageCode, cancellationToken)
            .ConfigureAwait(false);

        return new ProviderTranslation(answer.TranslatedText, answer.SourceLanguageCode);
    }

    /// <summary>
    /// Releases the resources the provider owns.
    /// </summary>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
