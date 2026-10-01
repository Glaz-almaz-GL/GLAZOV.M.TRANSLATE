using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Providers.Common;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace GLAZOV.M.TRANSLATE.Providers.Google.Internal;

/// <summary>
/// Performs the HTTP call and the response reading required to translate text
/// through the internal <c>batchexecute</c> service of the Google Translate web
/// page.
/// </summary>
/// <remarks>
/// <para>
/// This is the provider's engine: it contains the business logic of the
/// operation and is not part of the public API. Consumers depend on
/// <see cref="GoogleBatchExecuteTranslationProvider"/> instead.
/// </para>
/// <para>
/// The service wraps its answer twice: the body is a sequence of length-prefixed
/// chunks, the first chunk is a JSON array, and inside it the translation is a
/// JSON document written as a string. The positions inside the document are
/// undocumented, so every step checks the shape it expects and fails with a
/// <see cref="ProviderException"/> naming the step instead of reading past the
/// end of an array.
/// </para>
/// <para>
/// This type is thread-safe as long as the supplied <see cref="HttpClient"/>
/// is.
/// </para>
/// </remarks>
internal sealed class GoogleBatchExecuteEngine : ProviderEngine
{
    private const string Path = "/_/TranslateWebserverUi/data/batchexecute";
    private const string ResponsePrefix = ")]}'";
    private const string SuccessMarker = "wrb.fr";

    private readonly GoogleBatchExecuteOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleBatchExecuteEngine"/> class.
    /// </summary>
    /// <param name="options">The settings of the engine.</param>
    /// <param name="httpClient">
    /// The HTTP client used to send requests. The engine does not own its
    /// lifetime.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> or <paramref name="httpClient"/>
    /// is <see langword="null"/>.
    /// </exception>
    public GoogleBatchExecuteEngine(GoogleBatchExecuteOptions options, HttpClient httpClient)
        : base(GoogleBatchExecuteProvider.Name, httpClient)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleBatchExecuteEngine"/>
    /// class with an <see cref="HttpClient"/> of its own.
    /// </summary>
    /// <param name="options">The settings of the engine.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    public GoogleBatchExecuteEngine(GoogleBatchExecuteOptions options)
        : base(GoogleBatchExecuteProvider.Name)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
    }

    /// <summary>
    /// Translates text from one language code to another.
    /// </summary>
    /// <param name="text">The text to translate.</param>
    /// <param name="sourceLanguageCode">
    /// The Google code of the source language, or <c>"auto"</c> to request
    /// automatic detection.
    /// </param>
    /// <param name="targetLanguageCode">The Google code of the target language.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that completes with the translated text and the code of the source
    /// language Google reports, which is <see langword="null"/> when it reports
    /// none.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when a parameter is empty or white-space, or when
    /// <paramref name="text"/> is longer than the configured limit.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the request fails, when Google refuses it, or when the answer
    /// does not have the expected shape.
    /// </exception>
    public async Task<(string TranslatedText, string? SourceLanguageCode)> TranslateAsync(
        string text,
        string sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceLanguageCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetLanguageCode);

        if (text.Length > _options.MaxTextLength)
        {
            throw new ArgumentException(
                $"The text is {text.Length.ToString(CultureInfo.InvariantCulture)} characters long; the limit is {_options.MaxTextLength.ToString(CultureInfo.InvariantCulture)}.",
                nameof(text));
        }

        string query = string.Join(
            '&',
            $"rpcids={Uri.EscapeDataString(_options.RpcId)}",
            // The service accepts any number here; it only has to look like a session.
            $"f.sid={Random.Shared.NextInt64(1, long.MaxValue).ToString(CultureInfo.InvariantCulture)}",
            $"bl={Uri.EscapeDataString(_options.BuildLabel)}",
            "hl=en-US",
            "soc-app=1",
            "soc-platform=1",
            "soc-device=1",
            $"_reqid={Random.Shared.Next(0, 100000).ToString(CultureInfo.InvariantCulture)}",
            "rt=c");

        using HttpRequestMessage request = new(HttpMethod.Post, new Uri($"{_options.ServiceUrl}{Path}?{query}"))
        {
            Content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("f.req", BuildRequestBody(text, sourceLanguageCode, targetLanguageCode)),
            ]),
        };

        request.Headers.TryAddWithoutValidation("X-Same-Domain", "1");
        request.Headers.TryAddWithoutValidation("Origin", _options.ServiceUrl);
        request.Headers.TryAddWithoutValidation("Referer", $"{_options.ServiceUrl}/");

        string body;

        try
        {
            using HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw Refused((int)response.StatusCode, response.ReasonPhrase);
            }

            body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException exception)
        {
            throw RequestFailed(exception);
        }

        return ReadAnswer(body, text);
    }

    /// <summary>
    /// Writes the <c>f.req</c> form field: the call wrapped in three arrays, with
    /// its arguments written once more as a JSON string.
    /// </summary>
    private string BuildRequestBody(string text, string sourceLanguageCode, string targetLanguageCode)
    {
        string arguments = WriteJson(writer =>
        {
            writer.WriteStartArray();
            writer.WriteStartArray();
            writer.WriteStringValue(text);
            writer.WriteStringValue(sourceLanguageCode);
            writer.WriteStringValue(targetLanguageCode);
            writer.WriteBooleanValue(true);
            writer.WriteEndArray();
            writer.WriteStartArray();
            writer.WriteNullValue();
            writer.WriteEndArray();
            writer.WriteEndArray();
        });

        return WriteJson(writer =>
        {
            writer.WriteStartArray();
            writer.WriteStartArray();
            writer.WriteStartArray();
            writer.WriteStringValue(_options.RpcId);
            writer.WriteStringValue(arguments);
            writer.WriteNullValue();
            writer.WriteStringValue("generic");
            writer.WriteEndArray();
            writer.WriteEndArray();
            writer.WriteEndArray();
        });
    }

    private static string WriteJson(Action<Utf8JsonWriter> write)
    {
        using MemoryStream stream = new();

        using (Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            write(writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Reads the translation and the detected source language out of the answer.
    /// </summary>
    private (string TranslatedText, string? SourceLanguageCode) ReadAnswer(string body, string originalText)
    {
        try
        {
            using JsonDocument chunk = JsonDocument.Parse(FirstChunk(body));

            JsonElement? call = null;

            foreach (JsonElement entry in chunk.RootElement.EnumerateArray())
            {
                if (entry.ValueKind == JsonValueKind.Array
                    && entry.GetArrayLength() > 2
                    && entry[0].ValueKind == JsonValueKind.String
                    && entry[0].GetString() == SuccessMarker
                    && entry[1].ValueKind == JsonValueKind.String
                    && entry[1].GetString() == _options.RpcId)
                {
                    call = entry;
                    break;
                }
            }

            if (call is not { } found)
            {
                throw new ProviderException(ProviderName, $"{ProviderName} returned no answer to the call '{_options.RpcId}'.");
            }

            if (found[2].ValueKind != JsonValueKind.String)
            {
                // The refusal is a code in the sixth position, as in [3] for a malformed call.
                int? code = found.GetArrayLength() > 5
                    && found[5].ValueKind == JsonValueKind.Array
                    && found[5].GetArrayLength() > 0
                    && found[5][0].ValueKind == JsonValueKind.Number
                        ? found[5][0].GetInt32()
                        : null;

                throw code is { } refusal
                    ? RefusedWithCode(refusal, "the call was not accepted")
                    : new ProviderException(ProviderName, $"{ProviderName} returned an empty answer.");
            }

            using JsonDocument document = JsonDocument.Parse(found[2].GetString()!);

            JsonElement sentences = document.RootElement[1][0][0][5];

            string translation = JoinSentences(sentences, originalText);

            string? sourceCode = document.RootElement.GetArrayLength() > 2 && document.RootElement[2].ValueKind == JsonValueKind.String
                ? document.RootElement[2].GetString()
                : document.RootElement[1].GetArrayLength() > 3 && document.RootElement[1][3].ValueKind == JsonValueKind.String
                    ? document.RootElement[1][3].GetString()
                    : null;

            return (translation, sourceCode);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or IndexOutOfRangeException or ArgumentOutOfRangeException or KeyNotFoundException)
        {
            throw UnreadableAnswer(exception);
        }
    }

    /// <summary>
    /// Takes the first chunk of the answer: the first line that starts a JSON
    /// array, after the protective prefix.
    /// </summary>
    private string FirstChunk(string body)
    {
        string trimmed = body.StartsWith(ResponsePrefix, StringComparison.Ordinal) ? body[ResponsePrefix.Length..] : body;

        foreach (string line in trimmed.Split('\n'))
        {
            if (line.StartsWith("[[", StringComparison.Ordinal))
            {
                return line;
            }
        }

        throw new ProviderException(ProviderName, $"{ProviderName} returned an answer with no data.");
    }

    /// <summary>
    /// Joins the translated sentences. The service puts line breaks inside the
    /// sentences but drops the spaces between them, so a space that stood
    /// between the source sentences is put back.
    /// </summary>
    private static string JoinSentences(JsonElement sentences, string originalText)
    {
        StringBuilder result = new();
        int cursor = 0;
        bool first = true;

        foreach (JsonElement sentence in sentences.EnumerateArray())
        {
            string? translated = sentence[0].ValueKind == JsonValueKind.String ? sentence[0].GetString() : null;

            if (string.IsNullOrEmpty(translated))
            {
                continue;
            }

            string? source = sentence.GetArrayLength() > 6 && sentence[6].ValueKind == JsonValueKind.String
                ? sentence[6].GetString()
                : null;

            int found = string.IsNullOrEmpty(source) ? -1 : originalText.IndexOf(source, cursor, StringComparison.Ordinal);

            if (!first && found > cursor)
            {
                string gap = originalText[cursor..found];

                bool plainGap = gap.All(c => c is ' ' or '\t');

                if (plainGap
                    && !char.IsWhiteSpace(translated[0])
                    && result.Length > 0
                    && !char.IsWhiteSpace(result[^1]))
                {
                    result.Append(' ');
                }
            }

            result.Append(translated);

            if (found >= 0)
            {
                cursor = found + source!.Length;
            }

            first = false;
        }

        return result.ToString();
    }
}
