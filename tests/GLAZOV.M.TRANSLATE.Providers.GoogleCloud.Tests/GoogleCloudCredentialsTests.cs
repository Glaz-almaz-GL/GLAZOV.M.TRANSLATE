namespace GLAZOV.M.TRANSLATE.Providers.GoogleCloud.Tests;

/// <summary>
/// Verifies the API key of Google Cloud: what it refuses and what it keeps to
/// itself.
/// </summary>
public sealed class GoogleCloudCredentialsTests
{
    [Fact]
    public void Constructor_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new GoogleCloudCredentials(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Blank_ThrowsArgumentException(string apiKey)
    {
        Assert.Throws<ArgumentException>(() => new GoogleCloudCredentials(apiKey));
    }

    [Fact]
    public void Constructor_TrimsWhatWasPasted()
    {
        Assert.Equal("AIza-key", new GoogleCloudCredentials(" AIza-key\n").ApiKey);
    }

    [Fact]
    public void ToString_NeverShowsTheKey()
    {
        Assert.DoesNotContain("AIza-key", new GoogleCloudCredentials("AIza-key").ToString(), StringComparison.Ordinal);
    }
}
