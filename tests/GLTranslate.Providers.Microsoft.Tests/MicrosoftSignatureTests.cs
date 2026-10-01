using GLTranslate.Providers.Microsoft.Internal;

namespace GLTranslate.Providers.Microsoft.Tests;

/// <summary>
/// Verifies the shape and the stability of the <c>X-MT-Signature</c> value.
/// </summary>
public sealed class MicrosoftSignatureTests
{
    private static readonly DateTimeOffset FixedTimestamp = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid FixedNonce = new("0123456789abcdef0123456789abcdef");

    private const string Url = "api.cognitive.microsofttranslator.com/translate?api-version=3.0&to=ru";

    [Fact]
    public void Create_KnownInput_ReturnsKnownSignature()
    {
        string signature = MicrosoftSignature.Create(Url, FixedTimestamp, FixedNonce);

        Assert.Equal(
            "MSTranslatorAndroidApp::QKcSpauCu9ske8GBgB2liREWddnSX/3kgXFdf+xI6Iw=::Mon, 01 Jan 2024 12:00:00GMT::0123456789abcdef0123456789abcdef",
            signature);
    }

    [Fact]
    public void Create_SameInput_IsDeterministic()
    {
        string first = MicrosoftSignature.Create(Url, FixedTimestamp, FixedNonce);
        string second = MicrosoftSignature.Create(Url, FixedTimestamp, FixedNonce);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Create_DifferentUrl_ProducesDifferentSignature()
    {
        string first = MicrosoftSignature.Create(Url, FixedTimestamp, FixedNonce);
        string second = MicrosoftSignature.Create($"{Url}&from=en", FixedTimestamp, FixedNonce);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Create_DifferentNonce_ProducesDifferentSignature()
    {
        string first = MicrosoftSignature.Create(Url, FixedTimestamp, FixedNonce);
        string second = MicrosoftSignature.Create(Url, FixedTimestamp, Guid.NewGuid());

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Create_CarriesTheTimestampAndTheNonce()
    {
        string[] parts = MicrosoftSignature.Create(Url, FixedTimestamp, FixedNonce).Split("::");

        Assert.Equal(4, parts.Length);
        Assert.Equal("MSTranslatorAndroidApp", parts[0]);
        Assert.Equal("Mon, 01 Jan 2024 12:00:00GMT", parts[2]);
        Assert.Equal("0123456789abcdef0123456789abcdef", parts[3]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyUrl_ThrowsArgumentException(string url)
    {
        Assert.Throws<ArgumentException>(() => MicrosoftSignature.Create(url, FixedTimestamp, FixedNonce));
    }

    [Fact]
    public void Create_NullUrl_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => MicrosoftSignature.Create(null!, FixedTimestamp, FixedNonce));
    }
}
