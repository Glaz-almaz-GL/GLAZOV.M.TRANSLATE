using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;

namespace GLTranslate.Providers.Common.Tests;

/// <summary>
/// Verifies the text and markup translation providers' shared part: what is
/// asked of a derived provider, and how the result is put together from what it
/// answers.
/// </summary>
public sealed class TranslationProviderBaseTests
{
    private static readonly LanguageCodeResolver Languages = new("Fake", [new("ja", "jp")], [new("jp", "ja")]);

    private sealed class FakeTextProvider(Func<string, string?, string, ProviderTranslation> translate)
        : TextTranslationProviderBase(Languages)
    {
        public override string Name => "Fake";

        public List<(string Text, string? Source, string Target)> Asked { get; } = [];

        protected override Task<ProviderTranslation> TranslateAsync(
            string text,
            string? sourceLanguageCode,
            string targetLanguageCode,
            CancellationToken cancellationToken)
        {
            Asked.Add((text, sourceLanguageCode, targetLanguageCode));

            return Task.FromResult(translate(text, sourceLanguageCode, targetLanguageCode));
        }
    }

    private sealed class FakeMarkupProvider(Func<string, string?, string, ProviderTranslation> translate)
        : MarkupTranslationProviderBase(Languages)
    {
        public override string Name => "Fake";

        protected override Task<ProviderTranslation> TranslateAsync(
            string markup,
            string? sourceLanguageCode,
            string targetLanguageCode,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(translate(markup, sourceLanguageCode, targetLanguageCode));
        }
    }

    [Fact]
    public void Constructor_NullResolver_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new NullResolverText());
        Assert.Throws<ArgumentNullException>(() => new NullResolverMarkup());
    }

    private sealed class NullResolverText : TextTranslationProviderBase
    {
        public NullResolverText()
            : base(null!)
        {
        }

        public override string Name => "Fake";

        protected override Task<ProviderTranslation> TranslateAsync(string text, string? sourceLanguageCode, string targetLanguageCode, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class NullResolverMarkup : MarkupTranslationProviderBase
    {
        public NullResolverMarkup()
            : base(null!)
        {
        }

        public override string Name => "Fake";

        protected override Task<ProviderTranslation> TranslateAsync(string markup, string? sourceLanguageCode, string targetLanguageCode, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    [Fact]
    public async Task Text_NullRequest_ThrowsArgumentNullException()
    {
        FakeTextProvider provider = new((_, _, _) => new ProviderTranslation("x", null));

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.ExecuteAsync(null!));
    }

    [Fact]
    public async Task Text_ExplicitSource_AsksInTheProvidersTermsAndKeepsTheLanguage()
    {
        FakeTextProvider provider = new((_, _, _) => new ProviderTranslation("こんにちは", "en"));

        TextTranslationRequest request = new(new ProviderText("Hello"), new LanguageId("japanese"), new LanguageId("english"));
        TextTranslationResult result = await provider.ExecuteAsync(request);

        (string text, string? source, string target) = Assert.Single(provider.Asked);

        Assert.Equal("Hello", text);
        Assert.Equal("en", source);
        Assert.Equal("jp", target);

        Assert.Equal("こんにちは", result.TranslatedText.Value);
        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.Equal("japanese", result.TargetLanguageId.Value);
        Assert.False(result.WasSourceLanguageDetected);
        Assert.Equal(request.Id, result.RequestId);
    }

    [Fact]
    public async Task Text_NoSource_AsksForDetectionAndReportsWhatWasDetected()
    {
        FakeTextProvider provider = new((_, _, _) => new ProviderTranslation("Hello", "jp"));

        TextTranslationResult result = await provider.ExecuteAsync(
            new TextTranslationRequest(new ProviderText("こんにちは"), new LanguageId("english")));

        Assert.Null(Assert.Single(provider.Asked).Source);
        Assert.Equal("japanese", result.SourceLanguageId.Value);
        Assert.True(result.WasSourceLanguageDetected);
    }

    [Fact]
    public async Task Text_NoSourceAndNothingDetected_ThrowsProviderException()
    {
        FakeTextProvider provider = new((_, _, _) => new ProviderTranslation("Hello", null));

        await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new TextTranslationRequest(new ProviderText("こんにちは"), new LanguageId("english"))));
    }

    [Fact]
    public async Task Text_LanguageUnknownToGLTranslate_ThrowsProviderExceptionBeforeAskingTheProvider()
    {
        FakeTextProvider provider = new((_, _, _) => new ProviderTranslation("x", null));

        await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new TextTranslationRequest(new ProviderText("Hello"), new LanguageId("klingon-of-the-deep"))));

        Assert.Empty(provider.Asked);
    }

    [Fact]
    public async Task Markup_ExplicitSource_KeepsItAndReturnsTheMarkup()
    {
        FakeMarkupProvider provider = new((_, _, _) => new ProviderTranslation("<b>Привет</b>", null));

        MarkupTranslationRequest request = new(
            new ProviderMarkup("<b>Hello</b>"),
            new LanguageId("russian"),
            new LanguageId("english"));

        MarkupTranslationResult result = await provider.ExecuteAsync(request);

        Assert.Equal("<b>Привет</b>", result.TranslatedMarkup.Value);
        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.False(result.WasSourceLanguageDetected);
        Assert.Equal(request.Id, result.RequestId);
    }

    [Fact]
    public async Task Markup_NoSource_ReportsWhatWasDetected()
    {
        FakeMarkupProvider provider = new((_, _, _) => new ProviderTranslation("<b>Hello</b>", "jp"));

        MarkupTranslationResult result = await provider.ExecuteAsync(
            new MarkupTranslationRequest(new ProviderMarkup("<b>こんにちは</b>"), new LanguageId("english")));

        Assert.Equal("japanese", result.SourceLanguageId.Value);
        Assert.True(result.WasSourceLanguageDetected);
    }

    [Fact]
    public async Task Markup_NullRequest_ThrowsArgumentNullException()
    {
        FakeMarkupProvider provider = new((_, _, _) => new ProviderTranslation("x", null));

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.ExecuteAsync(null!));
    }

    [Fact]
    public async Task Markup_NoSourceAndNothingDetected_ThrowsProviderException()
    {
        FakeMarkupProvider provider = new((_, _, _) => new ProviderTranslation("x", null));

        await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new MarkupTranslationRequest(new ProviderMarkup("<b>Hello</b>"), new LanguageId("russian"))));
    }
}
