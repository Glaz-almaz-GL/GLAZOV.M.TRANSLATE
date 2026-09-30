using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;
using GLTranslate.Providers.GoogleCloud.Internal;
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
public sealed class GoogleCloudImageTranslationProvider : IImageTranslationProvider, IDisposable
{
    private readonly GoogleCloudEngine _engine;

    /// <inheritdoc/>
    public string Name => GoogleCloudProvider.Name;

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
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new GoogleCloudEngine(credentials, httpClient);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when a language of the request is unknown to GLTranslate, when
    /// Google refuses a request, or when an answer cannot be read.
    /// </exception>
    public async Task<ImageTranslationResult> ExecuteAsync(
        ImageTranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string targetCode = GoogleCloudLanguageCodeResolver.ToGoogleCloudCode(request.TargetLanguageId);

        string? sourceCode = request.SourceLanguageId is null
            ? null
            : GoogleCloudLanguageCodeResolver.ToGoogleCloudCode(request.SourceLanguageId);

        GoogleCloudTextAnnotation? text = await _engine
            .RecognizeAsync(request.Image.Content.AsMemory(), sourceCode, cancellationToken)
            .ConfigureAwait(false);

        GoogleCloudPage? page = text?.Pages?.FirstOrDefault();

        ImmutableArray<RecognizedLine> recognized = page is null ? [] : GoogleCloudTextLines.Read(page);

        if (recognized.IsEmpty)
        {
            // An image with no text on it is not a failure: there is simply
            // nothing to translate, and no language to report but the one asked
            // for, or the one Vision suspects, or the target when it suspects none.
            return new ImageTranslationResult(
                request.Id,
                [],
                request.SourceLanguageId ?? ResolveDetected(page, request.TargetLanguageId),
                request.TargetLanguageId,
                wasSourceLanguageDetected: request.SourceLanguageId is null);
        }

        (IReadOnlyList<string> translations, string? detectedSourceCode) = await _engine
            .TranslateAsync([.. recognized.Select(line => line.Text)], isMarkup: false, sourceCode, targetCode, cancellationToken)
            .ConfigureAwait(false);

        ImmutableArray<TranslatedLine>.Builder lines = ImmutableArray.CreateBuilder<TranslatedLine>(recognized.Length);

        for (int index = 0; index < recognized.Length; index++)
        {
            RecognizedLine line = recognized[index];

            lines.Add(new TranslatedLine(line.Text, translations[index], line.Bounds, line.Words));
        }

        LanguageId sourceLanguageId = request.SourceLanguageId
            ?? (string.IsNullOrWhiteSpace(detectedSourceCode)
                ? ResolveDetected(page, request.TargetLanguageId)
                : GoogleCloudLanguageCodeResolver.FromGoogleCloudCode(detectedSourceCode));

        return new ImageTranslationResult(
            request.Id,
            lines.MoveToImmutable(),
            sourceLanguageId,
            request.TargetLanguageId,
            wasSourceLanguageDetected: request.SourceLanguageId is null);
    }

    private static LanguageId ResolveDetected(GoogleCloudPage? page, LanguageId fallback)
    {
        string? code = page?.Property?.DetectedLanguages?.FirstOrDefault()?.LanguageCode;

        return string.IsNullOrWhiteSpace(code)
            ? fallback
            : GoogleCloudLanguageCodeResolver.FromGoogleCloudCode(code);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
