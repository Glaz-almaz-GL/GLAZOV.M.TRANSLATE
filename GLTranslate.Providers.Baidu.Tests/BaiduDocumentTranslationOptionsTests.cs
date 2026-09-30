namespace GLTranslate.Providers.Baidu.Tests;

/// <summary>
/// Verifies how a document translation is allowed to wait.
/// </summary>
public sealed class BaiduDocumentTranslationOptionsTests
{
    [Fact]
    public void Default_WaitsTwoSecondsBetweenQuestionsForTenMinutes()
    {
        Assert.Equal(TimeSpan.FromSeconds(2), BaiduDocumentTranslationOptions.Default.PollInterval);
        Assert.Equal(TimeSpan.FromMinutes(10), BaiduDocumentTranslationOptions.Default.Timeout);
    }

    [Fact]
    public void Constructor_GivenValues_KeepsThem()
    {
        BaiduDocumentTranslationOptions options = new(TimeSpan.FromSeconds(10), TimeSpan.FromHours(1));

        Assert.Equal(TimeSpan.FromSeconds(10), options.PollInterval);
        Assert.Equal(TimeSpan.FromHours(1), options.Timeout);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_IntervalNotPositive_ThrowsArgumentOutOfRangeException(int seconds)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BaiduDocumentTranslationOptions(TimeSpan.FromSeconds(seconds)));
    }

    [Fact]
    public void Constructor_TimeoutShorterThanInterval_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BaiduDocumentTranslationOptions(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5)));
    }
}
