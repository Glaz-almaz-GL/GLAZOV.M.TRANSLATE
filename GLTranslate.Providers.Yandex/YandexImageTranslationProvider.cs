using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;
using GLTranslate.Providers.Yandex.Internal;
using System.Collections.Immutable;

namespace GLTranslate.Providers.Yandex;

/// <summary>
/// Reads the text on an image and translates it with Yandex.
/// </summary>
/// <remarks>
/// <para>
/// This takes two calls: one to the recognition endpoint, which returns the
/// lines it found and where each of them sits, and one to the translation
/// endpoint, which translates all of them at once. Every line comes back with
/// its place on the image, so a caller can draw the translation over it.
/// </para>
/// <para>
/// The provider is thread-safe as long as the <see cref="HttpClient"/> it was
/// given is.
/// </para>
/// </remarks>
public sealed class YandexImageTranslationProvider : IImageTranslationProvider, IDisposable
{
    private readonly YandexEngine _engine;

    /// <inheritdoc/>
    public string Name => YandexProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="YandexImageTranslationProvider"/> class.
    /// </summary>
    public YandexImageTranslationProvider()
    {
        _engine = new YandexEngine();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YandexImageTranslationProvider"/>
    /// class with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient"/> is <see langword="null"/>.
    /// </exception>
    public YandexImageTranslationProvider(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new YandexEngine(httpClient);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when a language of the request is unknown to GLTranslate, when
    /// Yandex cannot read the image or refuses the direction, or when an
    /// endpoint answers with something the provider cannot read.
    /// </exception>
    public async Task<ImageTranslationResult> ExecuteAsync(
        ImageTranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string targetCode = YandexLanguageCodeResolver.ToYandexCode(request.TargetLanguageId);

        string? sourceCode = request.SourceLanguageId is null
            ? null
            : YandexLanguageCodeResolver.ToYandexCode(request.SourceLanguageId);

        YandexOcrData recognized = await _engine
            .RecognizeAsync(
                request.Image.Content.AsMemory(),
                request.Image.MediaType,
                request.Image.FileName,
                sourceCode,
                cancellationToken)
            .ConfigureAwait(false);

        ImmutableArray<YandexOcrBox> lines = ReadLines(recognized);

        if (lines.IsEmpty)
        {
            // An image with no text on it is not a failure: there is simply
            // nothing to translate, and no language to report but the one asked
            // for or detected.
            return new ImageTranslationResult(
                request.Id,
                [],
                request.SourceLanguageId ?? ResolveDetected(recognized.DetectedLanguage, request.TargetLanguageId),
                request.TargetLanguageId,
                wasSourceLanguageDetected: request.SourceLanguageId is null);
        }

        (IReadOnlyList<string> translations, string resolvedSourceCode) = await _engine
            .TranslateAsync([.. lines.Select(line => line.Text!)], targetCode, sourceCode, cancellationToken)
            .ConfigureAwait(false);

        ImmutableArray<TranslatedLine>.Builder translated = ImmutableArray.CreateBuilder<TranslatedLine>(lines.Length);

        for (int index = 0; index < lines.Length; index++)
        {
            YandexOcrBox line = lines[index];

            translated.Add(new TranslatedLine(
                line.Text!,
                translations[index],
                ToBounds(line),
                [.. (line.Words ?? []).Where(word => !string.IsNullOrWhiteSpace(word.Text))
                    .Select(word => new RecognizedWord(word.Text!, ToBounds(word)))]));
        }

        return new ImageTranslationResult(
            request.Id,
            translated.MoveToImmutable(),
            request.SourceLanguageId ?? YandexLanguageCodeResolver.FromYandexCode(resolvedSourceCode),
            request.TargetLanguageId,
            wasSourceLanguageDetected: request.SourceLanguageId is null);
    }

    private static ImmutableArray<YandexOcrBox> ReadLines(YandexOcrData recognized)
    {
        return
        [
            .. (recognized.Blocks ?? [])
                .SelectMany(block => block.Boxes ?? [])
                .Where(box => !string.IsNullOrWhiteSpace(box.Text)),
        ];
    }

    private static LanguageId ResolveDetected(string? detectedLanguageCode, LanguageId fallback)
    {
        // The recognition endpoint names the language it saw even when it saw
        // no text it could read; when it names nothing, there is nothing better
        // to report than the language that was asked for.
        return string.IsNullOrWhiteSpace(detectedLanguageCode)
            ? fallback
            : YandexLanguageCodeResolver.FromYandexCode(detectedLanguageCode);
    }

    private static TextBounds ToBounds(YandexOcrBox box)
    {
        return new TextBounds(box.X, box.Y, box.Width, box.Height);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
