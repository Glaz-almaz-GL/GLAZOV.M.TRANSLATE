using GLAZOV.M.TRANSLATE.Abstractions.Translation;
using GLAZOV.M.TRANSLATE.Providers.Common;
using GLAZOV.M.TRANSLATE.Providers.Google.Internal;

namespace GLAZOV.M.TRANSLATE.Providers.Google;

/// <summary>
/// Translates text through the internal <c>batchexecute</c> service of the
/// Google Translate web page, the call the page itself makes.
/// </summary>
/// <remarks>
/// <para>
/// It is a second way to the same free Google Translate, next to
/// <see cref="GoogleTranslationProvider"/>. It needs no API key, no cookie, no
/// token and no particular HTTP version, which is why it is the way to turn to
/// when the older endpoint refuses a caller. It is undocumented, unsupported by
/// Google, and may change or stop working without notice; the parts most likely
/// to change are settings of <see cref="GoogleBatchExecuteOptions"/>.
/// </para>
/// <para>
/// One request carries one text of at most
/// <see cref="GoogleBatchExecuteOptions.MaxTextLength"/> characters. Line breaks
/// inside the text are kept.
/// </para>
/// <para>
/// Instances are immutable and thread-safe, provided the supplied
/// <see cref="HttpClient"/> is not mutated after construction.
/// </para>
/// </remarks>
public sealed class GoogleBatchExecuteTranslationProvider : TextTranslationProviderBase, IDisposable
{
    private readonly GoogleBatchExecuteEngine _engine;

    /// <inheritdoc/>
    public override string Name => GoogleBatchExecuteProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleBatchExecuteTranslationProvider"/>
    /// class with the default settings.
    /// </summary>
    public GoogleBatchExecuteTranslationProvider()
        : this(new GoogleBatchExecuteOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleBatchExecuteTranslationProvider"/>
    /// class with the given settings.
    /// </summary>
    /// <param name="options">The settings of the provider.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    public GoogleBatchExecuteTranslationProvider(GoogleBatchExecuteOptions options)
        : base(GoogleLanguageCodeResolver.Instance)
    {
        ArgumentNullException.ThrowIfNull(options);

        _engine = new GoogleBatchExecuteEngine(options);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleBatchExecuteTranslationProvider"/>
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
    public GoogleBatchExecuteTranslationProvider(GoogleBatchExecuteOptions options, HttpClient httpClient)
        : base(GoogleLanguageCodeResolver.Instance)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new GoogleBatchExecuteEngine(options, httpClient);
    }

    /// <inheritdoc/>
    protected override async Task<ProviderTranslation> TranslateAsync(
        string text,
        string? sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken)
    {
        (string translatedText, string? detectedSourceCode) = await _engine
            .TranslateAsync(text, sourceLanguageCode ?? GoogleLanguageCodeResolver.AutoDetectCode, targetLanguageCode, cancellationToken)
            .ConfigureAwait(false);

        return new ProviderTranslation(translatedText, detectedSourceCode);
    }

    /// <summary>
    /// Releases the resources the provider owns.
    /// </summary>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
