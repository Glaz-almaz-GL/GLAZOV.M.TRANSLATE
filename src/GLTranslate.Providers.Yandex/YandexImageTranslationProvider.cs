using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;
using GLTranslate.Providers.Yandex.Internal;
using GLTranslate.Providers.Common;
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
public sealed class YandexImageTranslationProvider : ImageTranslationProviderBase, IDisposable
{
    private readonly YandexEngine _engine;

    /// <inheritdoc/>
    public override string Name => YandexProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="YandexImageTranslationProvider"/> class.
    /// </summary>
    public YandexImageTranslationProvider()
        : base(YandexLanguageCodeResolver.Instance)
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
        : base(YandexLanguageCodeResolver.Instance)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new YandexEngine(httpClient);
    }

    /// <inheritdoc/>
    protected override async Task<ImageTranslation> TranslateAsync(
        ProviderImage image,
        string? sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken)
    {
        YandexOcrData recognized = await _engine
            .RecognizeAsync(
                image.Content.AsMemory(),
                image.MediaType,
                image.FileName,
                sourceLanguageCode,
                cancellationToken)
            .ConfigureAwait(false);

        ImmutableArray<YandexOcrBox> lines = ReadLines(recognized);

        if (lines.IsEmpty)
        {
            // The recognition endpoint names the language it saw even when it saw
            // no text it could read.
            return new ImageTranslation([], recognized.DetectedLanguage);
        }

        (IReadOnlyList<string> translations, string resolvedSourceCode) = await _engine
            .TranslateAsync([.. lines.Select(line => line.Text!)], targetLanguageCode, sourceLanguageCode, cancellationToken)
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

        return new ImageTranslation(translated.MoveToImmutable(), resolvedSourceCode);
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
