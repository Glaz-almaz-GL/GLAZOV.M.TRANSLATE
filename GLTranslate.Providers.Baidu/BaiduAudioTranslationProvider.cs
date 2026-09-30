using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.TextToSpeech;
using GLTranslate.Abstractions.Translation;
using GLTranslate.Providers.Baidu.Internal;

namespace GLTranslate.Providers.Baidu;

/// <summary>
/// Listens to a recording of speech and translates it with the Baidu Translate
/// open platform.
/// </summary>
/// <remarks>
/// <para>
/// Baidu hears PCM, WAV, AMR and M4A recordings of less than 60 seconds and 4
/// MB, sampled at 16 kHz with 16 bits in one channel. Chinese and Cantonese
/// may come in any of the four formats; English, Japanese, Korean, Russian,
/// German, French, Thai, Portuguese, Spanish and Arabic as PCM only. The
/// recording is told by its <see cref="ProviderAudio.ContentType"/>:
/// <c>audio/wav</c>, <c>audio/amr</c>, <c>audio/mp4</c> (or <c>audio/m4a</c>)
/// and <c>audio/L16</c> (or <c>audio/pcm</c>).
/// </para>
/// <para>
/// Baidu cannot tell which language is spoken, so the request must name it;
/// a request that does not is refused before anything is sent. The result
/// carries the translation spoken aloud as an MP3 in
/// <see cref="AudioTranslationResult.Speech"/>.
/// </para>
/// <para>
/// The provider is thread-safe as long as the <see cref="HttpClient"/> it was
/// given is.
/// </para>
/// </remarks>
public sealed class BaiduAudioTranslationProvider : IAudioTranslationProvider, IDisposable
{
    private readonly BaiduAudioEngine _engine;

    /// <inheritdoc/>
    public string Name => BaiduProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaiduAudioTranslationProvider"/> class.
    /// </summary>
    /// <param name="credentials">
    /// The application the requests are made for.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="credentials"/> is <see langword="null"/>.
    /// </exception>
    public BaiduAudioTranslationProvider(BaiduCredentials credentials)
    {
        _engine = new BaiduAudioEngine(credentials);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaiduAudioTranslationProvider"/>
    /// class with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="credentials">
    /// The application the requests are made for.
    /// </param>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when an argument is <see langword="null"/>.
    /// </exception>
    public BaiduAudioTranslationProvider(BaiduCredentials credentials, HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new BaiduAudioEngine(credentials, httpClient);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the request names no source language, when a language is
    /// unknown to GLTranslate, when the recording is of a type Baidu does not
    /// hear or is too large, when Baidu refuses the request or hears nothing,
    /// or when the answer cannot be read.
    /// </exception>
    public async Task<AudioTranslationResult> ExecuteAsync(
        AudioTranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.SourceLanguageId is null)
        {
            // Baidu has no detection of the language of speech.
            throw new ProviderException(
                BaiduProvider.Name,
                "Baidu cannot tell which language is spoken: name the source language of the request.");
        }

        string format = ToFormat(request.Audio.ContentType.Value);
        string sourceCode = BaiduLanguageCodeResolver.ToBaiduCode(request.SourceLanguageId);
        string targetCode = BaiduLanguageCodeResolver.ToBaiduCode(request.TargetLanguageId);

        BaiduSpeechData data = await _engine
            .TranslateAsync(request.Audio.Content.AsMemory(), format, sourceCode, targetCode, cancellationToken)
            .ConfigureAwait(false);

        SpokenTranslation? speech = string.IsNullOrWhiteSpace(data.TargetSpeech)
            ? null
            : new SpokenTranslation(ReadSpeech(data.TargetSpeech), AudioContentType.Mp3);

        return new AudioTranslationResult(
            request.Id,
            new ProviderText(data.Source!),
            new ProviderText(data.Target!),
            speech,
            request.SourceLanguageId,
            request.TargetLanguageId,
            wasSourceLanguageDetected: false);
    }

    private static string ToFormat(string contentType)
    {
        // A content type may carry parameters, as in "audio/L16;rate=16000".
        string mediaType = contentType.Split(';', 2)[0].Trim().ToLowerInvariant();

        return mediaType switch
        {
            "audio/wav" or "audio/x-wav" or "audio/wave" or "audio/vnd.wave" => "wav",
            "audio/amr" => "amr",
            "audio/mp4" or "audio/m4a" or "audio/x-m4a" => "m4a",
            "audio/l16" or "audio/pcm" or "audio/x-pcm" => "pcm",
            _ => throw new ProviderException(
                BaiduProvider.Name,
                $"Baidu hears recordings in PCM, WAV, AMR and M4A only, not '{contentType}'."),
        };
    }

    private static byte[] ReadSpeech(string base64)
    {
        try
        {
            return Convert.FromBase64String(base64);
        }
        catch (FormatException exception)
        {
            throw new ProviderException(
                BaiduProvider.Name,
                "Baidu returned a spoken translation it cannot read.",
                exception);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
