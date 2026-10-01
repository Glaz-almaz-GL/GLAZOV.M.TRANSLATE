using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;

namespace GLTranslate.Providers.Common.Tests;

/// <summary>
/// Verifies how a provider's language codes are turned into languages and
/// back: the lookup itself and what is built on it.
/// </summary>
public sealed class LanguageCodeResolverTests
{
    private static readonly LanguageCodeResolver Resolver = new(
        "Fake",
        [new("ja", "jp")],
        [new("jp", "ja")]);

    [Fact]
    public void ToProviderCode_LanguageWithAnOwnCode_ReturnsIt()
    {
        Assert.Equal("jp", Resolver.ToProviderCode(new LanguageId("japanese")));
    }

    [Fact]
    public void ToProviderCode_LanguageWithoutOne_ReturnsTheIsoCode()
    {
        Assert.Equal("en", Resolver.ToProviderCode(new LanguageId("english")));
    }

    [Fact]
    public void ToProviderCode_UnknownLanguage_ThrowsProviderExceptionOfTheProvider()
    {
        ProviderException exception = Assert.Throws<ProviderException>(
            () => Resolver.ToProviderCode(new LanguageId("klingon-of-the-deep")));

        Assert.Equal("Fake", exception.ProviderName);
    }

    [Theory]
    [InlineData("pt-BR", "portuguese")]
    [InlineData("zh-Hant", "chinese")]
    [InlineData("EN-gb", "english")]
    public void FromProviderCode_CodeWithARegionOrScript_IsReadAsItsLanguage(string code, string expected)
    {
        Assert.Equal(expected, Resolver.FromProviderCode(code).Value);
    }

    [Fact]
    public void FromProviderCode_OwnCodeOfTheProvider_ReturnsTheLanguage()
    {
        Assert.Equal("japanese", Resolver.FromProviderCode("jp").Value);
    }

    [Theory]
    [InlineData("zz")]
    [InlineData("zz-YY")]
    public void FromProviderCode_UnknownCode_ThrowsProviderException(string code)
    {
        Assert.Throws<ProviderException>(() => Resolver.FromProviderCode(code));
    }

    [Fact]
    public void ResolveSource_LanguageNamedByTheRequest_IsKeptWhateverTheProviderSaid()
    {
        LanguageId requested = new("english");

        Assert.Same(requested, Resolver.ResolveSource(requested, "jp"));
    }

    [Fact]
    public void ResolveSource_NoneNamed_IsTheLanguageTheProviderDetected()
    {
        Assert.Equal("japanese", Resolver.ResolveSource(null, "jp").Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ResolveSource_NoneNamedAndNoneDetected_ThrowsProviderException(string? detected)
    {
        ProviderException exception = Assert.Throws<ProviderException>(() => Resolver.ResolveSource(null, detected));

        Assert.Equal("Fake", exception.ProviderName);
        Assert.Contains("no source language", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolveSource_DetectedCodeUnknown_ThrowsProviderException()
    {
        Assert.Throws<ProviderException>(() => Resolver.ResolveSource(null, "zz"));
    }
}
