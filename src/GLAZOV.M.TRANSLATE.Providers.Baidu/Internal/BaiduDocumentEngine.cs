using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace GLAZOV.M.TRANSLATE.Providers.Baidu.Internal;

/// <summary>
/// Performs the document translation calls against the Baidu Translate open
/// platform.
/// </summary>
/// <remarks>
/// <para>
/// This type is the business logic of the Baidu document translation provider
/// and is not part of the public API.
/// </para>
/// <para>
/// Baidu translates a document as a job: one call submits it and names the
/// job, another asks how the job is going, and when it is done the translated
/// document is downloaded from an address of Baidu's. The engine does all of
/// it and returns only when the document is in hand.
/// </para>
/// </remarks>
internal sealed class BaiduDocumentEngine : BaiduSignedEngine
{
    private const string SubmitUrl = "https://fanyi-api.baidu.com/transapi/doctrans/createjob/trans";
    private const string QueryUrl = "https://fanyi-api.baidu.com/transapi/doctrans/query/trans";

    private const string AutoLanguage = "auto";

    // The platform takes a document of 50 MB at most.
    private const int MaxDocumentLength = 50 * 1024 * 1024;

    private const int JobRunning = 0;
    private const int JobSucceeded = 1;

    private static readonly HashSet<string> Formats = new(StringComparer.Ordinal)
    {
        "doc", "docx", "xls", "xlsx", "ppt", "pptx", "xml", "html", "htm", "txt", "pdf",
    };

    private readonly BaiduDocumentTranslationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaiduDocumentEngine"/> class
    /// with an <see cref="HttpClient"/> of its own.
    /// </summary>
    /// <param name="credentials">
    /// The application the calls are made for.
    /// </param>
    /// <param name="options">
    /// How to wait for the job.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when an argument is <see langword="null"/>.
    /// </exception>
    public BaiduDocumentEngine(BaiduCredentials credentials, BaiduDocumentTranslationOptions options)
        : base(credentials)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaiduDocumentEngine"/> class
    /// with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="credentials">
    /// The application the calls are made for.
    /// </param>
    /// <param name="options">
    /// How to wait for the job.
    /// </param>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when an argument is <see langword="null"/>.
    /// </exception>
    public BaiduDocumentEngine(BaiduCredentials credentials, BaiduDocumentTranslationOptions options, HttpClient httpClient)
        : base(credentials, httpClient)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
    }

    /// <summary>
    /// Translates a document.
    /// </summary>
    /// <param name="content">
    /// The bytes of the document.
    /// </param>
    /// <param name="fileName">
    /// The name of the file of the document, with its extension.
    /// </param>
    /// <param name="format">
    /// The extension of the document without a dot and in lowercase.
    /// </param>
    /// <param name="sourceLanguageCode">
    /// The Baidu code of the language the document is written in, or
    /// <see langword="null"/> to let Baidu detect it.
    /// </param>
    /// <param name="targetLanguageCode">
    /// The Baidu code of the language to translate into.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation, and with it the
    /// waiting.
    /// </param>
    /// <returns>
    /// The translated document, its format, and the code of the language it
    /// was translated from when Baidu reported one.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the document is empty or an argument is empty or consists
    /// only of white-space characters.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the engine has been disposed.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the document is of a kind Baidu does not translate or is too
    /// large, when a request fails, when Baidu refuses it or fails the job,
    /// when the job does not finish in time, or when an answer cannot be read.
    /// </exception>
    public async Task<(byte[] Content, string Format, string? SourceLanguageCode)> TranslateAsync(
        ReadOnlyMemory<byte> content,
        string fileName,
        string format,
        string? sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(format);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetLanguageCode);

        if (content.IsEmpty)
        {
            throw new ArgumentException("A document cannot be empty.", nameof(content));
        }

        if (!Formats.Contains(format))
        {
            throw new ProviderException(
                BaiduProvider.Name,
                $"Baidu does not translate documents of the format '{format}'.");
        }

        if (content.Length > MaxDocumentLength)
        {
            throw new ProviderException(BaiduProvider.Name, "Baidu translates documents of 50 MB at most.");
        }

        long jobId = await SubmitAsync(
            content,
            fileName,
            format,
            sourceLanguageCode ?? AutoLanguage,
            targetLanguageCode,
            cancellationToken)
            .ConfigureAwait(false);

        BaiduJobData done = await WaitForAsync(jobId, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(done.FileUrl))
        {
            throw new ProviderException(BaiduProvider.Name, "Baidu finished the job but gave no document.");
        }

        byte[] translated = await DownloadAsync(done.FileUrl, cancellationToken).ConfigureAwait(false);

        // Baidu may give a document back in another format than it was sent in,
        // and says which.
        string translatedFormat = string.IsNullOrWhiteSpace(done.OutputFormat)
            ? format
            : done.OutputFormat.TrimStart('.').ToLowerInvariant();

        return (translated, translatedFormat, done.From);
    }

    private async Task<long> SubmitAsync(
        ReadOnlyMemory<byte> content,
        string fileName,
        string format,
        string sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken)
    {
        ArrayBufferWriter<byte> buffer = new();

        using (Utf8JsonWriter writer = new(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("from", sourceLanguageCode);
            writer.WriteString("to", targetLanguageCode);
            writer.WriteStartObject("input");
            writer.WriteBase64String("content", content.Span);
            writer.WriteString("format", format);
            writer.WriteString("filename", fileName);
            writer.WriteEndObject();

            // An empty format asks for the one Baidu gives by default: the
            // same as the document's own, or the nearest one it can write.
            writer.WriteStartObject("output");
            writer.WriteString("format", string.Empty);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        BaiduJobResponse? answer = await PostSignedAsync(
            SubmitUrl,
            System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan),
            signedPayload: null,
            BaiduJsonContext.Default.BaiduJobResponse,
            cancellationToken)
            .ConfigureAwait(false);

        return ReadJob(answer).RequestId;
    }

    private async Task<BaiduJobData> WaitForAsync(long jobId, CancellationToken cancellationToken)
    {
        long started = Stopwatch.GetTimestamp();

        string body = $$"""{"requestId":{{jobId.ToString(CultureInfo.InvariantCulture)}}}""";

        while (true)
        {
            await Task.Delay(_options.PollInterval, cancellationToken).ConfigureAwait(false);

            BaiduJobResponse? answer = await PostSignedAsync(
                QueryUrl,
                body,
                signedPayload: null,
                BaiduJsonContext.Default.BaiduJobResponse,
                cancellationToken)
                .ConfigureAwait(false);

            BaiduJobData data = ReadJob(answer);

            if (data.Status == JobSucceeded)
            {
                return data;
            }

            if (data.Status != JobRunning)
            {
                throw new ProviderException(
                    BaiduProvider.Name,
                    string.IsNullOrWhiteSpace(data.Reason)
                        ? "Baidu failed to translate the document."
                        : $"Baidu failed to translate the document: {data.Reason}");
            }

            if (Stopwatch.GetElapsedTime(started) >= _options.Timeout)
            {
                throw new ProviderException(
                    BaiduProvider.Name,
                    $"Baidu did not finish translating the document within {_options.Timeout}.");
            }
        }
    }

    private async Task<byte[]> DownloadAsync(string url, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            throw new ProviderException(BaiduProvider.Name, "Baidu gave an address of the document that cannot be read.");
        }

        try
        {
            using HttpResponseMessage response = await HttpClient
                .GetAsync(uri, cancellationToken)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException exception)
        {
            throw RequestFailed(exception);
        }
    }

    private BaiduJobData ReadJob(BaiduJobResponse? answer)
    {
        if (answer is null)
        {
            // Empty response
            throw new ProviderException(BaiduProvider.Name, "Baidu returned an empty response.");
        }

        Ensure(answer.Code, answer.Message);

        return answer.Data ?? throw new ProviderException(BaiduProvider.Name, "Baidu returned no data about the job.");
    }
}
