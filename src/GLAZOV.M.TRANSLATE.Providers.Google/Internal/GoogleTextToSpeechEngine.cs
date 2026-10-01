using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Providers.Common;

namespace GLAZOV.M.TRANSLATE.Providers.Google.Internal;

/// <summary>
/// Performs the HTTP calls required to synthesize speech through the free
/// Google Translate text-to-speech web endpoint.
/// </summary>
/// <remarks>
/// <para>
/// This is the provider's engine: it contains the business logic of the
/// operation and is not part of the public API. Consumers depend on
/// <see cref="GoogleTextToSpeechProvider"/> instead.
/// </para>
/// <para>
/// The endpoint rejects requests whose text exceeds 200 characters, so
/// longer text is split into multiple chunks (without breaking words),
/// synthesized independently, and concatenated. Concatenating raw MPEG
/// audio streams this way produces a file that plays back correctly.
/// </para>
/// <para>
/// This type is thread-safe as long as the supplied <see cref="HttpClient"/>
/// is thread-safe, which is the case for any <see cref="HttpClient"/> not
/// otherwise mutated after construction.
/// </para>
/// </remarks>
internal sealed class GoogleTextToSpeechEngine : ProviderEngine
{
    private const string ApiEndpoint = "https://translate.google.com/translate_tts";
    private const int MaxChunkLength = 200;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleTextToSpeechEngine"/>
    /// class with an <see cref="HttpClient"/> of its own.
    /// </summary>
    public GoogleTextToSpeechEngine()
        : base(GoogleProvider.Name)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleTextToSpeechEngine"/> class.
    /// </summary>
    /// <param name="httpClient">
    /// The HTTP client used to send requests to the Google Translate
    /// text-to-speech web endpoint. The engine does not own its lifetime.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient"/> is <see langword="null"/>.
    /// </exception>
    public GoogleTextToSpeechEngine(HttpClient httpClient)
        : base(GoogleProvider.Name, httpClient)
    {
    }

    /// <summary>
    /// Synthesizes speech for the specified text.
    /// </summary>
    /// <param name="text">
    /// The text to synthesize.
    /// </param>
    /// <param name="languageCode">
    /// The ISO 639-1 code of the voice language.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task that completes with the synthesized audio, encoded as MP3.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="text"/> or <paramref name="languageCode"/>
    /// is empty or consists only of white-space characters.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the request fails.
    /// </exception>
    public async Task<byte[]> SynthesizeAsync(
        string text,
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(languageCode);

        IReadOnlyList<string> chunks = SplitIntoChunks(text, MaxChunkLength);

        Task<byte[]>[] tasks = new Task<byte[]>[chunks.Count];

        for (int i = 0; i < chunks.Count; i++)
        {
            tasks[i] = FetchChunkAsync(chunks[i], languageCode, i, chunks.Count, cancellationToken);
        }

        byte[][] chunkAudio = await Task.WhenAll(tasks).ConfigureAwait(false);

        if (chunkAudio.Length == 1)
        {
            return chunkAudio[0];
        }

        using MemoryStream stream = new();

        foreach (byte[] chunk in chunkAudio)
        {
            stream.Write(chunk);
        }

        return stream.ToArray();
    }

    private async Task<byte[]> FetchChunkAsync(
        string chunk,
        string languageCode,
        int index,
        int total,
        CancellationToken cancellationToken)
    {
        string token = GoogleTokenGenerator.Generate(chunk);

        string url = $"{ApiEndpoint}?ie=UTF-8" +
                     $"&q={Uri.EscapeDataString(chunk)}" +
                     $"&tl={Uri.EscapeDataString(languageCode)}" +
                     "&ttsspeed=1" +
                     $"&total={total}&idx={index}" +
                     "&client=tw-ob" +
                     $"&textlen={chunk.Length}" +
                     $"&tk={Uri.EscapeDataString(token)}";

        try
        {
            using HttpResponseMessage response = await HttpClient
                .GetAsync(new Uri(url), cancellationToken)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException exception)
        {
            throw RequestFailed(exception);
        }
    }

    private static IReadOnlyList<string> SplitIntoChunks(string text, int maxLength)
    {
        List<string> chunks = [];
        int start = 0;

        while (start < text.Length)
        {
            int remaining = text.Length - start;

            if (remaining <= maxLength)
            {
                chunks.Add(text[start..]);
                break;
            }

            int length = maxLength;
            int lastSpace = text.LastIndexOf(' ', start + length - 1, length);

            if (lastSpace > start)
            {
                length = lastSpace - start;
            }

            chunks.Add(text[start..(start + length)]);
            start += length;

            while (start < text.Length && text[start] == ' ')
            {
                start++;
            }
        }

        return chunks;
    }
}
