namespace GLAZOV.M.TRANSLATE.Providers.YandexCloud.Tests;

/// <summary>
/// Verifies the identity of a Yandex Cloud caller: what it refuses and what it
/// keeps to itself.
/// </summary>
public sealed class YandexCloudCredentialsTests
{
    [Fact]
    public void Constructor_NullKey_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new YandexCloudCredentials(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankKey_ThrowsArgumentException(string apiKey)
    {
        Assert.Throws<ArgumentException>(() => new YandexCloudCredentials(apiKey));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_BlankFolder_ThrowsArgumentException(string folderId)
    {
        Assert.Throws<ArgumentException>(() => new YandexCloudCredentials("key", folderId));
    }

    [Fact]
    public void Constructor_NoFolder_LeavesItNull()
    {
        Assert.Null(new YandexCloudCredentials("key").FolderId);
    }

    [Fact]
    public void Constructor_TrimsWhatWasPasted()
    {
        YandexCloudCredentials credentials = new(" AQVN-key\n", " b1g123 ");

        Assert.Equal("AQVN-key", credentials.ApiKey);
        Assert.Equal("b1g123", credentials.FolderId);
    }

    [Fact]
    public void ToString_NeverShowsTheKey()
    {
        string text = new YandexCloudCredentials("AQVN-key", "b1g123").ToString();

        Assert.DoesNotContain("AQVN-key", text, StringComparison.Ordinal);
        Assert.Contains("b1g123", text, StringComparison.Ordinal);
    }
}
