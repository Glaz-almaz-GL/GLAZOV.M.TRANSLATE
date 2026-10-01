using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Abstractions.TextToSpeech;
using GLAZOV.M.TRANSLATE.Providers.Google.Internal;

namespace GLAZOV.M.TRANSLATE.Providers.Google;

/// <summary>
/// Speaks text through the internal <c>batchexecute</c> service of the Google
/// Translate web page, the call the page itself makes for its speaker button.
/// </summary>
/// <remarks>
/// <para>
/// It is a second way to the same free Google voice, next to
/// <see cref="GoogleTextToSpeechProvider"/>, and needs no token, cookie or
/// particular HTTP version. Google speaks one voice per language and offers no
/// choice, so a request that names a voice is refused. Text longer than the
/// service accepts in one request is cut at spaces and the pieces are spoken
/// one by one.
/// </para>
/// <para>
/// The service is undocumented, unsupported by Google, and may change or stop
/// working without notice; the parts most likely to change are settings of
/// <see cref="GoogleBatchExecuteOptions"/>.
/// </para>
/// <para>
/// Instances are immutable and thread-safe, provided the supplied
/// <see cref="HttpClient"/> is not mutated after construction.
/// </para>
/// </remarks>
public sealed class GoogleBatchExecuteTextToSpeechProvider : ITextToSpeechProvider, IDisposable
{
    private readonly GoogleBatchExecuteEngine _engine;

    /// <inheritdoc/>
    public string Name => GoogleBatchExecuteProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleBatchExecuteTextToSpeechProvider"/>
    /// class with the default settings.
    /// </summary>
    public GoogleBatchExecuteTextToSpeechProvider()
        : this(new GoogleBatchExecuteOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleBatchExecuteTextToSpeechProvider"/>
    /// class with the given settings.
    /// </summary>
    /// <param name="options">The settings of the provider.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    public GoogleBatchExecuteTextToSpeechProvider(GoogleBatchExecuteOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _engine = new GoogleBatchExecuteEngine(options);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleBatchExecuteTextToSpeechProvider"/>
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
    public GoogleBatchExecuteTextToSpeechProvider(GoogleBatchExecuteOptions options, HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new GoogleBatchExecuteEngine(options, httpClient);
    }

    /// <inheritdoc/>
    /// <exception cref="ProviderException">
    /// Thrown when the request names a voice, when Google cannot speak the
    /// language, or when the call fails.
    /// </exception>
    public async Task<TextToSpeechResult> ExecuteAsync(TextToSpeechRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.VoiceName is not null)
        {
            throw new ProviderException(
                GoogleBatchExecuteProvider.Name,
                "Google Translate offers no choice of voice: leave the voice of the request unset.");
        }

        GoogleTextToSpeechProvider.EnsureSpoken(request.LanguageId);

        byte[] audioData = await _engine
            .SpeakAsync(request.Text.Value, GoogleLanguageCodeResolver.ToGoogleCode(request.LanguageId), cancellationToken)
            .ConfigureAwait(false);

        return new TextToSpeechResult(request.Id, audioData, AudioContentType.Mp3, request.LanguageId);
    }

    /// <summary>
    /// Releases the resources the provider owns.
    /// </summary>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
