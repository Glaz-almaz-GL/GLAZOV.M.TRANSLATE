using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;
using GLTranslate.Providers.YandexCloud.Internal;
using System.Collections.Immutable;

namespace GLTranslate.Providers.YandexCloud;

/// <summary>
/// Reads the text on an image with Yandex Cloud Vision OCR and translates it
/// with Yandex Cloud Translate.
/// </summary>
/// <remarks>
/// <para>
/// Yandex Cloud has no API that translates an image in one call, so the
/// provider uses two: Vision OCR reads the lines and where they are, and
/// Translate translates all the lines together. Both are billed to the folder
/// of the key, which must have both services available.
/// </para>
/// <para>
/// Every line comes back with its place on the image and with its words, so a
/// caller can draw the translation over it. Vision reads PNG and JPEG here. The
/// provider asks Yandex not to keep the image.
/// </para>
/// <para>
/// The provider is thread-safe as long as the <see cref="HttpClient"/> it was
/// given is.
/// </para>
/// </remarks>
public sealed class YandexCloudImageTranslationProvider : IImageTranslationProvider, IDisposable
{
    private readonly YandexCloudEngine _engine;

    /// <inheritdoc/>
    public string Name => YandexCloudProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="YandexCloudImageTranslationProvider"/> class.
    /// </summary>
    /// <param name="credentials">
    /// The key the requests are made with.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="credentials"/> is <see langword="null"/>.
    /// </exception>
    public YandexCloudImageTranslationProvider(YandexCloudCredentials credentials)
    {
        _engine = new YandexCloudEngine(credentials);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YandexCloudImageTranslationProvider"/>
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
    public YandexCloudImageTranslationProvider(YandexCloudCredentials credentials, HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new YandexCloudEngine(credentials, httpClient);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when a language of the request is unknown to GLTranslate, when
    /// the image is of a type Yandex does not read here, when Yandex refuses a
    /// request, or when an answer cannot be read.
    /// </exception>
    public async Task<ImageTranslationResult> ExecuteAsync(
        ImageTranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string targetCode = YandexCloudLanguageCodeResolver.ToYandexCloudCode(request.TargetLanguageId);

        string? sourceCode = request.SourceLanguageId is null
            ? null
            : YandexCloudLanguageCodeResolver.ToYandexCloudCode(request.SourceLanguageId);

        YandexCloudTextAnnotation? text = await _engine
            .RecognizeAsync(request.Image.Content.AsMemory(), request.Image.MediaType, sourceCode, cancellationToken)
            .ConfigureAwait(false);

        YandexCloudLine[] recognized =
        [
            .. (text?.Blocks ?? [])
                .SelectMany(block => block.Lines ?? [])
                .Where(line => !string.IsNullOrWhiteSpace(line.Text)),
        ];

        if (recognized.Length == 0)
        {
            // An image with no text on it is not a failure: there is simply
            // nothing to translate, and no language to report but the one asked
            // for, or the one Yandex suspects, or the target when it suspects none.
            return new ImageTranslationResult(
                request.Id,
                [],
                request.SourceLanguageId ?? ResolveDetected(text, request.TargetLanguageId),
                request.TargetLanguageId,
                wasSourceLanguageDetected: request.SourceLanguageId is null);
        }

        (IReadOnlyList<string> translations, string? detectedSourceCode) = await _engine
            .TranslateAsync([.. recognized.Select(line => line.Text!.Trim())], isMarkup: false, sourceCode, targetCode, cancellationToken)
            .ConfigureAwait(false);

        ImmutableArray<TranslatedLine>.Builder lines = ImmutableArray.CreateBuilder<TranslatedLine>(recognized.Length);

        for (int index = 0; index < recognized.Length; index++)
        {
            YandexCloudLine line = recognized[index];

            lines.Add(new TranslatedLine(
                line.Text!.Trim(),
                translations[index],
                ToBounds(line.BoundingBox),
                [.. (line.Words ?? [])
                    .Where(word => !string.IsNullOrWhiteSpace(word.Text))
                    .Select(word => new RecognizedWord(word.Text!, ToBounds(word.BoundingBox)))]));
        }

        LanguageId sourceLanguageId = request.SourceLanguageId
            ?? (string.IsNullOrWhiteSpace(detectedSourceCode)
                ? ResolveDetected(text, request.TargetLanguageId)
                : YandexCloudLanguageCodeResolver.FromYandexCloudCode(detectedSourceCode));

        return new ImageTranslationResult(
            request.Id,
            lines.MoveToImmutable(),
            sourceLanguageId,
            request.TargetLanguageId,
            wasSourceLanguageDetected: request.SourceLanguageId is null);
    }

    private static LanguageId ResolveDetected(YandexCloudTextAnnotation? text, LanguageId fallback)
    {
        string? code = text?.Blocks?
            .SelectMany(block => block.Languages ?? [])
            .Select(language => language.LanguageCode)
            .FirstOrDefault(languageCode => !string.IsNullOrWhiteSpace(languageCode));

        return code is null ? fallback : YandexCloudLanguageCodeResolver.FromYandexCloudCode(code);
    }

    private static TextBounds ToBounds(YandexCloudPolygon? polygon)
    {
        IReadOnlyList<YandexCloudVertex> vertices = polygon?.Vertices ?? [];

        if (vertices.Count == 0)
        {
            return default;
        }

        int left = vertices.Min(vertex => vertex.X);
        int top = vertices.Min(vertex => vertex.Y);

        return new TextBounds(
            left,
            top,
            vertices.Max(vertex => vertex.X) - left,
            vertices.Max(vertex => vertex.Y) - top);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
