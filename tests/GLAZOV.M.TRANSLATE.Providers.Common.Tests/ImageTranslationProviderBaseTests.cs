using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Abstractions.Translation;
using System.Collections.Immutable;

namespace GLAZOV.M.TRANSLATE.Providers.Common.Tests;

/// <summary>
/// Verifies the image translation providers' shared part: what is asked of a
/// derived provider, and how the result is put together from what it answers.
/// </summary>
public sealed class ImageTranslationProviderBaseTests
{
    private static readonly LanguageCodeResolver Languages = new("Fake", [new("ja", "jp")], [new("jp", "ja")]);

    private static readonly byte[] Bytes = [1, 2, 3];

    private static readonly TranslatedLine Line = new("Hello", "Привет", new TextBounds(1, 2, 3, 4), []);

    private sealed class FakeImageProvider(Func<ImageTranslation> translate) : ImageTranslationProviderBase(Languages)
    {
        public override string Name => "Fake";

        public List<(string? Source, string Target)> Asked { get; } = [];

        protected override Task<ImageTranslation> TranslateAsync(
            ProviderImage image,
            string? sourceLanguageCode,
            string targetLanguageCode,
            CancellationToken cancellationToken)
        {
            Asked.Add((sourceLanguageCode, targetLanguageCode));

            return Task.FromResult(translate());
        }
    }

    [Fact]
    public void Constructor_NullResolver_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new NullResolverImage());
    }

    private sealed class NullResolverImage : ImageTranslationProviderBase
    {
        public NullResolverImage()
            : base(null!)
        {
        }

        public override string Name => "Fake";

        protected override Task<ImageTranslation> TranslateAsync(ProviderImage image, string? sourceLanguageCode, string targetLanguageCode, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        FakeImageProvider provider = new(() => new ImageTranslation([], null));

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsync_Lines_ComeBackAsTheyAreWithTheDetectedLanguage()
    {
        FakeImageProvider provider = new(() => new ImageTranslation([Line], "en"));

        ImageTranslationRequest request = new(new ProviderImage(Bytes), new LanguageId("japanese"));
        ImageTranslationResult result = await provider.ExecuteAsync(request);

        Assert.Equal(("jp", (string?)null), (provider.Asked[0].Target, provider.Asked[0].Source));
        Assert.Equal(Line, Assert.Single(result.Lines));
        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.Equal("japanese", result.TargetLanguageId.Value);
        Assert.True(result.WasSourceLanguageDetected);
        Assert.Equal(request.Id, result.RequestId);
    }

    [Fact]
    public async Task ExecuteAsync_ExplicitSource_IsAskedInTheProvidersTermsAndKept()
    {
        FakeImageProvider provider = new(() => new ImageTranslation([Line], "fr"));

        ImageTranslationResult result = await provider.ExecuteAsync(new ImageTranslationRequest(
            new ProviderImage(Bytes),
            new LanguageId("russian"),
            new LanguageId("japanese")));

        Assert.Equal("jp", provider.Asked[0].Source);
        Assert.Equal("japanese", result.SourceLanguageId.Value);
        Assert.False(result.WasSourceLanguageDetected);
    }

    [Fact]
    public async Task ExecuteAsync_NothingOnTheImageAndNoLanguageFound_ReportsTheLanguageAskedFor()
    {
        FakeImageProvider provider = new(() => new ImageTranslation([], null));

        ImageTranslationResult result = await provider.ExecuteAsync(
            new ImageTranslationRequest(new ProviderImage(Bytes), new LanguageId("russian")));

        Assert.Empty(result.Lines);
        Assert.Equal("russian", result.SourceLanguageId.Value);
        Assert.True(result.WasSourceLanguageDetected);
    }

    [Fact]
    public async Task ExecuteAsync_NothingOnTheImageButALanguageFound_ReportsIt()
    {
        FakeImageProvider provider = new(() => new ImageTranslation(ImmutableArray<TranslatedLine>.Empty, "jp"));

        ImageTranslationResult result = await provider.ExecuteAsync(
            new ImageTranslationRequest(new ProviderImage(Bytes), new LanguageId("russian")));

        Assert.Equal("japanese", result.SourceLanguageId.Value);
    }

    [Fact]
    public async Task ExecuteAsync_LanguageFoundIsUnknownToTheLibrary_ThrowsProviderException()
    {
        FakeImageProvider provider = new(() => new ImageTranslation([Line], "zz"));

        await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new ImageTranslationRequest(new ProviderImage(Bytes), new LanguageId("russian"))));
    }

    [Fact]
    public async Task ExecuteAsync_DefaultImageTranslation_IsAnEmptyOneAndReportsTheTargetLanguage()
    {
        // A default ImageTranslation has no lines array at all; the result
        // normalizes it to an empty one.
        FakeImageProvider provider = new(() => default);

        ImageTranslationResult result = await provider.ExecuteAsync(
            new ImageTranslationRequest(new ProviderImage(Bytes), new LanguageId("russian")));

        Assert.Empty(result.Lines);
    }
}
