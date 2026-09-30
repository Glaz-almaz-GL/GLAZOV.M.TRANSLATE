using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;
using GLTranslate.Providers.Baidu.Internal;
using System.Collections.Immutable;
using System.Globalization;

namespace GLTranslate.Providers.Baidu;

/// <summary>
/// Reads the text on an image and translates it with the Baidu Translate open
/// platform.
/// </summary>
/// <remarks>
/// <para>
/// Baidu does both in one call and answers with the segments of text it found,
/// each with its translation and where it sits, so a caller can draw the
/// translation over the image. It reports no words, so
/// <see cref="TranslatedLine.Words"/> of every line is empty.
/// </para>
/// <para>
/// Baidu reads PNG and JPEG only, up to 4 MB, at least 30 pixels on the short
/// side and at most 4096 on the long one. Its image service has to be switched
/// on for the application in the Baidu console, and reads fewer languages than
/// the text service does.
/// </para>
/// <para>
/// The provider is thread-safe as long as the <see cref="HttpClient"/> it was
/// given is.
/// </para>
/// </remarks>
public sealed class BaiduImageTranslationProvider : IImageTranslationProvider, IDisposable
{
    private readonly BaiduEngine _engine;

    /// <inheritdoc/>
    public string Name => BaiduProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaiduImageTranslationProvider"/> class.
    /// </summary>
    /// <param name="credentials">
    /// The application the requests are made for.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="credentials"/> is <see langword="null"/>.
    /// </exception>
    public BaiduImageTranslationProvider(BaiduCredentials credentials)
    {
        _engine = new BaiduEngine(credentials);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaiduImageTranslationProvider"/>
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
    public BaiduImageTranslationProvider(BaiduCredentials credentials, HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new BaiduEngine(credentials, httpClient);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when a language of the request is unknown to GLTranslate, when
    /// the image is of a type Baidu does not read, when Baidu refuses the
    /// request or reads nothing on the image, or when the answer cannot be
    /// read.
    /// </exception>
    public async Task<ImageTranslationResult> ExecuteAsync(
        ImageTranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string targetCode = BaiduLanguageCodeResolver.ToBaiduCode(request.TargetLanguageId);

        string? sourceCode = request.SourceLanguageId is null
            ? null
            : BaiduLanguageCodeResolver.ToBaiduCode(request.SourceLanguageId);

        BaiduPictureData data = await _engine
            .TranslatePictureAsync(
                request.Image.Content.AsMemory(),
                request.Image.MediaType,
                sourceCode,
                targetCode,
                cancellationToken)
            .ConfigureAwait(false);

        ImmutableArray<TranslatedLine> lines = ReadLines(data);

        return new ImageTranslationResult(
            request.Id,
            lines,
            request.SourceLanguageId ?? ResolveDetected(data.From, request.TargetLanguageId),
            request.TargetLanguageId,
            wasSourceLanguageDetected: request.SourceLanguageId is null);
    }

    private static ImmutableArray<TranslatedLine> ReadLines(BaiduPictureData data)
    {
        ImmutableArray<TranslatedLine>.Builder lines = ImmutableArray.CreateBuilder<TranslatedLine>();

        foreach (BaiduPictureSegment segment in data.Content ?? [])
        {
            if (string.IsNullOrWhiteSpace(segment.Source) || segment.Destination is null)
            {
                continue;
            }

            lines.Add(new TranslatedLine(
                segment.Source.Trim(),
                segment.Destination,
                ToBounds(segment.Rectangle),
                []));
        }

        return lines.ToImmutable();
    }

    private static LanguageId ResolveDetected(string? detectedLanguageCode, LanguageId fallback)
    {
        // Baidu names the language it read even when the image held nothing to
        // translate; when it names none, there is nothing better to report than
        // the language that was asked for.
        return string.IsNullOrWhiteSpace(detectedLanguageCode)
            ? fallback
            : BaiduLanguageCodeResolver.FromBaiduCode(detectedLanguageCode);
    }

    private static TextBounds ToBounds(string? rectangle)
    {
        // "left top width height"
        string[] parts = (rectangle ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 4
            || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int left)
            || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int top)
            || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int width)
            || !int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int height))
        {
            throw new ProviderException(
                BaiduProvider.Name,
                $"Baidu returned a position it cannot read: '{rectangle}'.");
        }

        return new TextBounds(left, top, width, height);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
