using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.TextToSpeech;
using GLTranslate.Providers.YandexCloud.Internal;

namespace GLTranslate.Providers.YandexCloud;

/// <summary>
/// Speaks text with the official Yandex SpeechKit API.
/// </summary>
/// <remarks>
/// <para>
/// SpeechKit speaks Russian, English, German, Kazakh and Uzbek through the API
/// this provider uses (version 1); a request for another language is refused
/// before anything is sent. Without a voice name the default voice of the
/// language speaks; a request may name another of Yandex's voices, such as
/// <c>filipp</c> for Russian.
/// </para>
/// <para>
/// SpeechKit speaks 5,000 characters at most in a request, and fewer when the
/// script takes several bytes a letter, because the request itself is limited
/// to 15 KB once encoded; a longer text is refused before anything is sent.
/// The recording is an MP3.
/// </para>
/// <para>
/// The provider is thread-safe as long as the <see cref="HttpClient"/> it was
/// given is.
/// </para>
/// </remarks>
public sealed class YandexCloudTextToSpeechProvider : ITextToSpeechProvider, IDisposable
{
    private readonly YandexCloudEngine _engine;

    /// <inheritdoc/>
    public string Name => YandexCloudProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="YandexCloudTextToSpeechProvider"/> class.
    /// </summary>
    /// <param name="credentials">
    /// The key the requests are made with.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="credentials"/> is <see langword="null"/>.
    /// </exception>
    public YandexCloudTextToSpeechProvider(YandexCloudCredentials credentials)
    {
        _engine = new YandexCloudEngine(credentials);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YandexCloudTextToSpeechProvider"/>
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
    public YandexCloudTextToSpeechProvider(YandexCloudCredentials credentials, HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new YandexCloudEngine(credentials, httpClient);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the language is unknown to GLTranslate or is one SpeechKit
    /// does not speak, when the text is too long, when Yandex refuses the
    /// request or answers with no audio.
    /// </exception>
    public async Task<TextToSpeechResult> ExecuteAsync(
        TextToSpeechRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string code = YandexCloudLanguageCodeResolver.ToYandexCloudCode(request.LanguageId);

        if (!YandexCloudSpeechVoices.TryGet(code, out string? language, out string? defaultVoice))
        {
            throw new ProviderException(
                YandexCloudProvider.Name,
                $"Yandex SpeechKit cannot speak language '{request.LanguageId.Value}'.");
        }

        byte[] audio = await _engine
            .SynthesizeAsync(request.Text.Value, language, request.VoiceName?.Value ?? defaultVoice, cancellationToken)
            .ConfigureAwait(false);

        return new TextToSpeechResult(request.Id, audio, AudioContentType.Mp3, request.LanguageId);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
