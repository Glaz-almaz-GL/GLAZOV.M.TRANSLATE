using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Abstractions.Translation;
using GLAZOV.M.TRANSLATE.Providers.YandexCloud.Internal;
using GLAZOV.M.TRANSLATE.Providers.Common;
using System.Collections.Immutable;

namespace GLAZOV.M.TRANSLATE.Providers.YandexCloud;

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
public sealed class YandexCloudImageTranslationProvider : ImageTranslationProviderBase, IDisposable
{
    private readonly YandexCloudEngine _engine;

    /// <inheritdoc/>
    public override string Name => YandexCloudProvider.Name;

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
        : base(YandexCloudLanguageCodeResolver.Instance)
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
        : base(YandexCloudLanguageCodeResolver.Instance)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new YandexCloudEngine(credentials, httpClient);
    }

    /// <inheritdoc/>
    protected override async Task<ImageTranslation> TranslateAsync(
        ProviderImage image,
        string? sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken)
    {
        YandexCloudTextAnnotation? text = await _engine
            .RecognizeAsync(image.Content.AsMemory(), image.MediaType, sourceLanguageCode, cancellationToken)
            .ConfigureAwait(false);

        YandexCloudLine[] recognized =
        [
            .. (text?.Blocks ?? [])
                .SelectMany(block => block.Lines ?? [])
                .Where(line => !string.IsNullOrWhiteSpace(line.Text)),
        ];

        string? suspected = text?.Blocks?
            .SelectMany(block => block.Languages ?? [])
            .Select(language => language.LanguageCode)
            .FirstOrDefault(languageCode => !string.IsNullOrWhiteSpace(languageCode));

        if (recognized.Length == 0)
        {
            return new ImageTranslation([], suspected);
        }

        (IReadOnlyList<string> translations, string? detectedSourceCode) = await _engine
            .TranslateAsync([.. recognized.Select(line => line.Text!.Trim())], isMarkup: false, sourceLanguageCode, targetLanguageCode, cancellationToken)
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

        // What Translate detected is the better witness of the language, since
        // it has read the lines; OCR's suspicion is the fallback.
        return new ImageTranslation(lines.MoveToImmutable(), detectedSourceCode ?? suspected);
    }

    private static TextBounds ToBounds(YandexCloudPolygon? polygon)
    {
        return TextBounds.Enclosing((polygon?.Vertices ?? []).Select(vertex => (vertex.X, vertex.Y)));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
