using GLTranslate.Abstractions.Providers;
using System.Buffers;
using System.Text.Json;

namespace GLTranslate.Providers.Baidu.Internal;

/// <summary>
/// Performs the speech translation calls against the Baidu Translate open
/// platform.
/// </summary>
/// <remarks>
/// This type is the business logic of the Baidu audio translation provider and
/// is not part of the public API. Baidu takes a whole recording in one call and
/// answers with what it heard, the translation, and the translation spoken.
/// </remarks>
internal sealed class BaiduAudioEngine : BaiduSignedEngine
{
    private const string SpeechUrl = "https://fanyi-api.baidu.com/api/trans/v2/voicetrans";

    // The platform takes 4 MB of audio at most, counted before it is encoded.
    private const int MaxAudioLength = 4 * 1024 * 1024;

    private static readonly HashSet<string> Formats = new(StringComparer.Ordinal)
    {
        "pcm", "wav", "amr", "m4a",
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="BaiduAudioEngine"/> class
    /// with an <see cref="HttpClient"/> of its own.
    /// </summary>
    /// <param name="credentials">
    /// The application the calls are made for.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="credentials"/> is <see langword="null"/>.
    /// </exception>
    public BaiduAudioEngine(BaiduCredentials credentials)
        : base(credentials)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaiduAudioEngine"/> class
    /// with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="credentials">
    /// The application the calls are made for.
    /// </param>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when an argument is <see langword="null"/>.
    /// </exception>
    public BaiduAudioEngine(BaiduCredentials credentials, HttpClient httpClient)
        : base(credentials, httpClient)
    {
    }

    /// <summary>
    /// Translates a recording of speech.
    /// </summary>
    /// <param name="audio">
    /// The bytes of the recording.
    /// </param>
    /// <param name="format">
    /// The format of the recording: <c>pcm</c>, <c>wav</c>, <c>amr</c> or
    /// <c>m4a</c>.
    /// </param>
    /// <param name="sourceLanguageCode">
    /// The Baidu code of the language spoken in the recording.
    /// </param>
    /// <param name="targetLanguageCode">
    /// The Baidu code of the language to translate into.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// What Baidu heard, its translation, and the translation spoken.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the recording is empty or an argument is empty or consists
    /// only of white-space characters.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the engine has been disposed.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the format is one Baidu does not hear or the recording is
    /// too large, when the request fails, when Baidu refuses it or hears
    /// nothing, or when the answer cannot be read.
    /// </exception>
    public async Task<BaiduSpeechData> TranslateAsync(
        ReadOnlyMemory<byte> audio,
        string format,
        string sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(format);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceLanguageCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetLanguageCode);

        if (audio.IsEmpty)
        {
            throw new ArgumentException("A recording cannot be empty.", nameof(audio));
        }

        if (!Formats.Contains(format))
        {
            throw new ProviderException(
                BaiduProvider.Name,
                $"Baidu hears recordings in PCM, WAV, AMR and M4A only, not '{format}'.");
        }

        if (audio.Length > MaxAudioLength)
        {
            throw new ProviderException(BaiduProvider.Name, "Baidu takes recordings of 4 MB at most.");
        }

        string voice = Convert.ToBase64String(audio.Span);

        ArrayBufferWriter<byte> buffer = new();

        using (Utf8JsonWriter writer = new(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("from", sourceLanguageCode);
            writer.WriteString("to", targetLanguageCode);
            writer.WriteString("format", format);
            writer.WriteString("voice", voice);
            writer.WriteEndObject();
        }

        // Unlike a document, a recording is signed by its encoded audio alone
        // and not by the whole body.
        BaiduSpeechResponse? answer = await PostSignedAsync(
            SpeechUrl,
            System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan),
            signedPayload: voice,
            BaiduJsonContext.Default.BaiduSpeechResponse,
            cancellationToken)
            .ConfigureAwait(false);

        if (answer is null)
        {
            throw new ProviderException(BaiduProvider.Name, "Baidu returned an empty response.");
        }

        Ensure(answer.Code, answer.Message);

        return answer.Data is { Source: not null, Target: not null } data
            ? data
            : throw new ProviderException(BaiduProvider.Name, "Baidu heard nothing in the recording.");
    }
}
