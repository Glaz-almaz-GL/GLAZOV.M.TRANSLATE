using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Abstractions.Transliteration;

namespace GLAZOV.M.TRANSLATE.Providers.Common.Tests;

/// <summary>
/// Verifies the transliteration providers' shared part: what is asked of a
/// derived provider, and how the result is put together from what it answers.
/// </summary>
public sealed class TransliterationProviderBaseTests
{
    private static readonly LanguageCodeResolver Languages = new("Fake", [new("ja", "jp")], [new("jp", "ja")]);

    private sealed class FakeProvider(Func<ProviderTransliteration> transliterate) : TransliterationProviderBase(Languages)
    {
        public override string Name => "Fake";

        public List<(string Text, LanguageId? Language, string? Code)> Asked { get; } = [];

        protected override Task<ProviderTransliteration> TransliterateAsync(
            string text,
            LanguageId? languageId,
            string? languageCode,
            CancellationToken cancellationToken)
        {
            Asked.Add((text, languageId, languageCode));

            return Task.FromResult(transliterate());
        }
    }

    private sealed class NullResolverProvider : TransliterationProviderBase
    {
        public NullResolverProvider()
            : base(null!)
        {
        }

        public override string Name => "Fake";

        protected override Task<ProviderTransliteration> TransliterateAsync(string text, LanguageId? languageId, string? languageCode, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    [Fact]
    public void Constructor_NullResolver_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new NullResolverProvider());
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        FakeProvider provider = new(() => new ProviderTransliteration("x", null));

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsync_NamedLanguage_IsAskedInTheProvidersTermsAndKept()
    {
        FakeProvider provider = new(() => new ProviderTransliteration("konnichiwa", "en"));

        LanguageId japanese = new("japanese");
        TransliterationRequest request = new(new ProviderText("こんにちは"), japanese);
        TransliterationResult result = await provider.ExecuteAsync(request);

        (string text, LanguageId? language, string? code) = Assert.Single(provider.Asked);

        Assert.Equal("こんにちは", text);
        Assert.Same(japanese, language);
        Assert.Equal("jp", code);

        Assert.Equal("konnichiwa", result.Transliteration.Value);
        Assert.Equal("japanese", result.LanguageId.Value);
        Assert.False(result.WasLanguageDetected);
        Assert.Equal(request.Id, result.RequestId);
    }

    [Fact]
    public async Task ExecuteAsync_NoLanguage_ReportsWhatWasDetected()
    {
        FakeProvider provider = new(() => new ProviderTransliteration("konnichiwa", "jp"));

        TransliterationResult result = await provider.ExecuteAsync(
            new TransliterationRequest(new ProviderText("こんにちは")));

        (_, LanguageId? language, string? code) = Assert.Single(provider.Asked);

        Assert.Null(language);
        Assert.Null(code);
        Assert.Equal("japanese", result.LanguageId.Value);
        Assert.True(result.WasLanguageDetected);
    }

    [Fact]
    public async Task ExecuteAsync_NoLanguageAndNothingDetected_ThrowsProviderException()
    {
        FakeProvider provider = new(() => new ProviderTransliteration("x", null));

        await Assert.ThrowsAsync<ProviderException>(
            () => provider.ExecuteAsync(new TransliterationRequest(new ProviderText("こんにちは"))));
    }

    [Fact]
    public async Task ExecuteAsync_LanguageUnknownToTheLibrary_ThrowsProviderExceptionBeforeAskingTheProvider()
    {
        FakeProvider provider = new(() => new ProviderTransliteration("x", null));

        await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new TransliterationRequest(new ProviderText("x"), new LanguageId("klingon-of-the-deep"))));

        Assert.Empty(provider.Asked);
    }
}
