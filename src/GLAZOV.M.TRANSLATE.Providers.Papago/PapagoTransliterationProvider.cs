using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Providers.Common;
using GLAZOV.M.TRANSLATE.Providers.Papago.Internal;

namespace GLAZOV.M.TRANSLATE.Providers.Papago;

/// <summary>
/// Writes text in the Latin script using the free web service of Naver Papago.
/// </summary>
/// <remarks>
/// <para>
/// Papago gives the transliteration of the source text as a by-product of a
/// translation, so the provider asks for a translation into English and keeps
/// only the transliteration. Text already written in the Latin script comes back
/// without one, and the provider reports that as a
/// <see cref="ProviderException"/> instead of returning the text unchanged.
/// </para>
/// <para>
/// Instances are immutable and thread-safe, provided the supplied
/// <see cref="HttpClient"/> is not mutated after construction.
/// </para>
/// </remarks>
public sealed class PapagoTransliterationProvider : TransliterationProviderBase, IDisposable
{
    // The language to translate into while asking for the transliteration. It only
    // has to be a language Papago translates into; the translation is discarded.
    private const string PivotLanguageCode = "en";

    private readonly PapagoEngine _engine;

    /// <inheritdoc/>
    public override string Name => PapagoProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="PapagoTransliterationProvider"/>
    /// class with the default settings.
    /// </summary>
    public PapagoTransliterationProvider()
        : this(new PapagoOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PapagoTransliterationProvider"/>
    /// class with the given settings.
    /// </summary>
    /// <param name="options">The settings of the provider.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    public PapagoTransliterationProvider(PapagoOptions options)
        : base(PapagoLanguageCodeResolver.Instance)
    {
        ArgumentNullException.ThrowIfNull(options);

        _engine = new PapagoEngine(options);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PapagoTransliterationProvider"/>
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
    public PapagoTransliterationProvider(PapagoOptions options, HttpClient httpClient)
        : base(PapagoLanguageCodeResolver.Instance)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new PapagoEngine(options, httpClient);
    }

    /// <inheritdoc/>
    protected override async Task<ProviderTransliteration> TransliterateAsync(
        string text,
        LanguageId? languageId,
        string? languageCode,
        CancellationToken cancellationToken)
    {
        PapagoAnswer answer = await _engine
            .TranslateAsync(text, languageCode ?? PapagoLanguageCodeResolver.AutoDetectCode, PivotLanguageCode, cancellationToken)
            .ConfigureAwait(false);

        if (answer.SourceTransliteration is not { } transliteratedText)
        {
            throw new ProviderException(
                PapagoProvider.Name,
                "Papago rendered nothing: the text is already written in the Latin script, or in one it does not romanize.");
        }

        return new ProviderTransliteration(transliteratedText, answer.SourceLanguageCode);
    }

    /// <summary>
    /// Releases the resources the provider owns.
    /// </summary>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
