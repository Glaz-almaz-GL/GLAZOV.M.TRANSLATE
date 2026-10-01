using GLTranslate.Abstractions.Translation;

namespace GLTranslate.Domain.Tests.Translation;

/// <summary>
/// Verifies how a polygon around a piece of text becomes a place on an image.
/// </summary>
public sealed class TextBoundsTests
{
    [Fact]
    public void Enclosing_CornersOfAnUprightBox_GiveThatBox()
    {
        TextBounds bounds = TextBounds.Enclosing([(10, 59), (384, 59), (384, 79), (10, 79)]);

        Assert.Equal(new TextBounds(10, 59, 374, 20), bounds);
    }

    [Fact]
    public void Enclosing_CornersOfATiltedQuadrilateral_GiveTheSmallestBoxAroundIt()
    {
        TextBounds bounds = TextBounds.Enclosing([(12, 5), (100, 0), (98, 30), (10, 34)]);

        Assert.Equal(new TextBounds(10, 0, 90, 34), bounds);
    }

    [Fact]
    public void Enclosing_PointsInAnyOrder_GiveTheSameBox()
    {
        TextBounds first = TextBounds.Enclosing([(1, 2), (9, 2), (9, 8), (1, 8)]);
        TextBounds second = TextBounds.Enclosing([(9, 8), (1, 2), (1, 8), (9, 2)]);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Enclosing_OnePoint_GivesABoxOfNoSizeThere()
    {
        Assert.Equal(new TextBounds(7, 3, 0, 0), TextBounds.Enclosing([(7, 3)]));
    }

    [Fact]
    public void Enclosing_NoPoints_GivesADefaultBox()
    {
        Assert.Equal(default, TextBounds.Enclosing([]));
    }

    [Fact]
    public void Enclosing_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => TextBounds.Enclosing(null!));
    }
}
