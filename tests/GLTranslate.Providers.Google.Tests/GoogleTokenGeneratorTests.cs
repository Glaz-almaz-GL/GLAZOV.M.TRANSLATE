using GLTranslate.Providers.Google.Internal;

namespace GLTranslate.Providers.Google.Tests;

public sealed class GoogleTokenGeneratorTests
{
    private static readonly DateTimeOffset FixedTimestamp = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Generate_SameTextAndTimestamp_IsDeterministic()
    {
        string first = GoogleTokenGenerator.Generate("Hello, world!", FixedTimestamp);
        string second = GoogleTokenGenerator.Generate("Hello, world!", FixedTimestamp);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Generate_DifferentText_ProducesDifferentTokens()
    {
        string first = GoogleTokenGenerator.Generate("Hello", FixedTimestamp);
        string second = GoogleTokenGenerator.Generate("Goodbye", FixedTimestamp);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Generate_DifferentHour_ProducesDifferentTokens()
    {
        string first = GoogleTokenGenerator.Generate("Hello", FixedTimestamp);
        string second = GoogleTokenGenerator.Generate("Hello", FixedTimestamp.AddHours(1));

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Generate_SameHourDifferentMinute_ProducesSameToken()
    {
        string first = GoogleTokenGenerator.Generate("Hello", FixedTimestamp);
        string second = GoogleTokenGenerator.Generate("Hello", FixedTimestamp.AddMinutes(30));

        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("A longer sentence with punctuation, numbers 123, and Unicode: привет мир!")]
    public void Generate_ReturnsTwoDotSeparatedIntegers(string text)
    {
        string token = GoogleTokenGenerator.Generate(text, FixedTimestamp);
        string[] parts = token.Split('.');

        Assert.Equal(2, parts.Length);
        Assert.True(long.TryParse(parts[0], out _));
        Assert.True(long.TryParse(parts[1], out _));
    }

    [Fact]
    public void Generate_NullText_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => GoogleTokenGenerator.Generate(null!));
    }

    [Theory]
    [MemberData(nameof(InvalidTimestamps))]
    public void Generate_InvalidTimestamp_ThrowsArgumentOutOfRangeException(DateTimeOffset timestamp)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GoogleTokenGenerator.Generate("Hello", timestamp));
    }

#pragma warning disable CA1825 // Нежелательность выделения массивов нулевой длины
    public static TheoryData<DateTimeOffset> InvalidTimestamps =>
    [
        DateTimeOffset.MinValue,
        DateTimeOffset.MaxValue
    ];
#pragma warning restore CA1825 // Нежелательность выделения массивов нулевой длины
}
