namespace GLAZOV.M.TRANSLATE.Providers.Baidu.Tests;

/// <summary>
/// Verifies the identity of a Baidu application: what it refuses and what it
/// keeps to itself.
/// </summary>
public sealed class BaiduCredentialsTests
{
    [Fact]
    public void Constructor_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new BaiduCredentials(null!, "key"));
        Assert.Throws<ArgumentNullException>(() => new BaiduCredentials("id", null!));
    }

    [Theory]
    [InlineData("", "key")]
    [InlineData("  ", "key")]
    [InlineData("id", "")]
    [InlineData("id", "  ")]
    public void Constructor_Blank_ThrowsArgumentException(string appId, string secretKey)
    {
        Assert.Throws<ArgumentException>(() => new BaiduCredentials(appId, secretKey));
    }

    [Fact]
    public void Constructor_TrimsWhatWasPasted()
    {
        BaiduCredentials credentials = new(" 2015063000000001 ", " 1234567890\n");

        Assert.Equal("2015063000000001", credentials.AppId);
        Assert.Equal("1234567890", credentials.SecretKey);
    }

    [Fact]
    public void ToString_NeverShowsTheSecretKey()
    {
        BaiduCredentials credentials = new("2015063000000001", "1234567890");

        string text = credentials.ToString();

        Assert.Contains("2015063000000001", text, StringComparison.Ordinal);
        Assert.DoesNotContain("1234567890", text, StringComparison.Ordinal);
    }
}
