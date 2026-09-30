using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;
using GLTranslate.Providers.GoogleCloud.Internal;
using GLTranslate.Providers.Common;
using System.Collections.Immutable;

namespace GLTranslate.Providers.GoogleCloud;

/// <summary>
/// Reads the text on an image with Google Cloud Vision and translates it with
/// Google Cloud Translation.
/// </summary>
/// <remarks>
/// <para>
/// Google has no API that translates an image: the translate.google.com page
/// does it through a call protected by a token only a browser makes. The
/// official route is two APIs, and this provider walks it: Vision reads the
/// words and where they are, the provider puts them back into lines, and
/// Translation translates all the lines together. Both are billed to the
/// project of the key, which must have both APIs enabled.
/// </para>
/// <para>
/// Every line comes back with its place on the image and with its words, so a
/// caller can draw the translation over it. Vision reads PNG, JPEG, GIF, WebP
/// and other common formats; Google limits the size of an image.
/// </para>
/// <para>
/// The provider is thread-safe as long as the <see cref="HttpClient"/> it was
/// given is.
/// </para>
/// </remarks>
public sealed class GoogleCloudImageTranslationProvider : ImageTranslationProviderBase, IDisposable
{
    private readonly GoogleCloudEngine _engine;

    /// <inheritdoc/>
    public override string Name => GoogleCloudProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleCloudImageTranslationProvider"/> class.
    /// </summary>
    /// <param name="credentials">
    /// The key the requests are made with.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="credentials"/> is <see langword="null"/>.
    /// </exception>
    public GoogleCloudImageTranslationProvider(GoogleCloudCredentials credentials)
        : base(GoogleCloudLanguageCodeResolver.Instance)
    {
        _engine = new GoogleCloudEngine(credentials);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleCloudImageTranslationProvider"/>
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
    public GoogleCloudImageTranslationProvider(GoogleCloudCredentials credentials, HttpClient httpClient)
        : base(GoogleCloudLanguageCodeResolver.Instance)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new GoogleCloudEngine(credentials, httpClient);
    }

    /// <inheritdoc/>
    protected override async Task<ImageTranslation> TranslateAsync(
        ProviderImage image,
        string? sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken)
    {
        GoogleCloudTextAnnotation? text = await _engine
            .RecognizeAsync(image.Content.AsMemory(), sourceLanguageCode, cancellationToken)
            .ConfigureAwait(false);

        GoogleCloudPage? page = text?.Pages?.FirstOrDefault();

        ImmutableArray<RecognizedLine> recognized = page is null ? [] : GoogleCloudTextLines.Read(page);

        string? suspected = page?.Property?.DetectedLanguages?.FirstOrDefault()?.LanguageCode;

        if (recognized.IsEmpty)
        {
            return new ImageTranslation([], suspected);
        }

        (IReadOnlyList<string> translations, string? detectedSourceCode) = await _engine
            .TranslateAsync([.. recognized.Select(line => line.Text)], isMarkup: false, sourceLanguageCode, targetLanguageCode, cancellationToken)
            .ConfigureAwait(false);

        ImmutableArray<TranslatedLine>.Builder lines = ImmutableArray.CreateBuilder<TranslatedLine>(recognized.Length);

        for (int index = 0; index < recognized.Length; index++)
        {
            RecognizedLine line = recognized[index];

            lines.Add(new TranslatedLine(line.Text, translations[index], line.Bounds, line.Words));
        }

        // What Translation detected is the better witness of the language, since
        // it has read the lines; Vision's suspicion is the fallback.
        return new ImageTranslation(lines.MoveToImmutable(), detectedSourceCode ?? suspected);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
