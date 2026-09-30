using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.TextToSpeech;
using GLTranslate.Providers.GoogleCloud.Internal;

namespace GLTranslate.Providers.GoogleCloud;

/// <summary>
/// Speaks text with the official Google Cloud Text-to-Speech API.
/// </summary>
/// <remarks>
/// <para>
/// Unlike the keyless Google provider, which gives one voice per language,
/// this one lets the request name a voice, such as <c>en-US-Neural2-A</c>;
/// the catalog of names is Google's. Without a name Google's default voice for
/// the language speaks: the provider asks which region the language is spoken
/// in by the first voice Google has for it, once for each language, and asks
/// no more for that language.
/// </para>
/// <para>
/// Google takes 5,000 bytes of text in a request; a longer text is refused
/// before anything is sent. The recording is an MP3.
/// </para>
/// <para>
/// The provider is thread-safe as long as the <see cref="HttpClient"/> it was
/// given is.
/// </para>
/// </remarks>
public sealed class GoogleCloudTextToSpeechProvider : ITextToSpeechProvider, IDisposable
{
    private readonly GoogleCloudEngine _engine;

    /// <inheritdoc/>
    public string Name => GoogleCloudProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleCloudTextToSpeechProvider"/> class.
    /// </summary>
    /// <param name="credentials">
    /// The key the requests are made with.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="credentials"/> is <see langword="null"/>.
    /// </exception>
    public GoogleCloudTextToSpeechProvider(GoogleCloudCredentials credentials)
    {
        _engine = new GoogleCloudEngine(credentials);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleCloudTextToSpeechProvider"/>
    /// class with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="credentials">
    /// The key the requests are made with.
    /// </param>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when an argument is <see langword="null"/>.
    /// </exception>
    public GoogleCloudTextToSpeechProvider(GoogleCloudCredentials credentials, HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new GoogleCloudEngine(credentials, httpClient);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the language is unknown to GLTranslate, when the voice name
    /// is not one of Google's, when the text is too long, when no voice speaks
    /// the language, when Google refuses the request, or when the answer
    /// cannot be read.
    /// </exception>
    public async Task<TextToSpeechResult> ExecuteAsync(
        TextToSpeechRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string languageCode = request.VoiceName is null
            ? GoogleCloudLanguageCodeResolver.ToGoogleCloudCode(request.LanguageId)
            : LanguageOfVoice(request.VoiceName.Value);

        byte[] audio = await _engine
            .SynthesizeAsync(request.Text.Value, languageCode, request.VoiceName?.Value, cancellationToken)
            .ConfigureAwait(false);

        return new TextToSpeechResult(request.Id, audio, AudioContentType.Mp3, request.LanguageId);
    }

    private static string LanguageOfVoice(string voiceName)
    {
        // Google names a voice after the language and region it speaks:
        // "en-US-Neural2-A" speaks "en-US".
        string[] parts = voiceName.Split('-');

        return parts.Length >= 3 && parts[0].Length > 0 && parts[1].Length > 0
            ? $"{parts[0]}-{parts[1]}"
            : throw new ProviderException(
                GoogleCloudProvider.Name,
                $"'{voiceName}' is not a Google Cloud voice name: they look like 'en-US-Neural2-A'.");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
