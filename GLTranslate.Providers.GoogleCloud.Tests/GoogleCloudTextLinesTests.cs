using GLTranslate.Abstractions.Translation;
using GLTranslate.Providers.GoogleCloud.Internal;
using System.Collections.Immutable;
using System.Text.Json;

namespace GLTranslate.Providers.GoogleCloud.Tests;

/// <summary>
/// Verifies how the words Vision read are put back into lines.
/// </summary>
public sealed class GoogleCloudTextLinesTests
{
    private static GoogleCloudPage PageOf(params string[] words)
    {
        GoogleCloudAnnotateResponse answer = JsonSerializer.Deserialize(
            VisionAnswer.Page(null, words),
            GoogleCloudJsonContext.Default.GoogleCloudAnnotateResponse)!;

        return answer.Responses![0].FullText!.Pages![0];
    }

    [Fact]
    public void Read_NullPage_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => GoogleCloudTextLines.Read(null!));
    }

    [Fact]
    public void Read_WordsOfOneLine_AreJoinedBySpacesAndBoxedTogether()
    {
        ImmutableArray<RecognizedLine> lines = GoogleCloudTextLines.Read(PageOf(
            VisionAnswer.Word("Good", 10, 20, 40, 12, "SPACE"),
            VisionAnswer.Word("morning", 55, 18, 70, 16, "LINE_BREAK")));

        RecognizedLine line = Assert.Single(lines);

        Assert.Equal("Good morning", line.Text);
        Assert.Equal(new TextBounds(10, 18, 115, 16), line.Bounds);
        Assert.Equal(2, line.Words.Length);
        Assert.Equal("Good", line.Words[0].Text);
        Assert.Equal(new TextBounds(10, 20, 40, 12), line.Words[0].Bounds);
    }

    [Theory]
    [InlineData("EOL_SURE_SPACE")]
    [InlineData("LINE_BREAK")]
    [InlineData("HYPHEN")]
    public void Read_BreakThatEndsALine_StartsANewLine(string breakType)
    {
        ImmutableArray<RecognizedLine> lines = GoogleCloudTextLines.Read(PageOf(
            VisionAnswer.Word("Good", 10, 20, 40, 12, breakType),
            VisionAnswer.Word("morning", 10, 40, 70, 12, "LINE_BREAK")));

        Assert.Equal(["Good", "morning"], lines.Select(line => line.Text));
    }

    [Fact]
    public void Read_WordsWithNoBreakBetweenThem_AreJoinedWithoutASpace()
    {
        // Scripts written without spaces: Vision reports no break between words.
        ImmutableArray<RecognizedLine> lines = GoogleCloudTextLines.Read(PageOf(
            VisionAnswer.Word("今天", 0, 0, 20, 10, null),
            VisionAnswer.Word("天气", 20, 0, 20, 10, "LINE_BREAK")));

        Assert.Equal("今天天气", Assert.Single(lines).Text);
    }

    [Fact]
    public void Read_ParagraphWithoutAFinalBreak_StillEndsItsLine()
    {
        ImmutableArray<RecognizedLine> lines = GoogleCloudTextLines.Read(PageOf(
            VisionAnswer.Word("Hello", 1, 2, 30, 10, "SPACE"),
            VisionAnswer.Word("world", 40, 2, 30, 10, null)));

        Assert.Equal("Hello world", Assert.Single(lines).Text);
    }

    [Fact]
    public void Read_WordWithNoSymbols_IsLeftOut()
    {
        ImmutableArray<RecognizedLine> lines = GoogleCloudTextLines.Read(PageOf(
            """{"symbols":[]}""",
            VisionAnswer.Word("Hi", 0, 0, 10, 10, "LINE_BREAK")));

        RecognizedLine line = Assert.Single(lines);

        Assert.Equal("Hi", line.Text);
        Assert.Single(line.Words);
    }

    [Fact]
    public void Read_PageWithNoBlocks_HasNoLines()
    {
        Assert.Empty(GoogleCloudTextLines.Read(new GoogleCloudPage()));
    }
}
