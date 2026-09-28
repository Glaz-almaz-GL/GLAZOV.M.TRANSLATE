using GLTranslate.Abstractions.Linguistics.Scripts;
using GLTranslate.Domain.Linguistics.Scripts;
using GLTranslate.Domain.Linguistics.Scripts.Code;

namespace GLTranslate.Domain.Tests.Linguistics.Scripts;

/// <summary>
/// Verifies construction-time validation, writing direction and equality
/// of the <see cref="Script"/> entity.
/// </summary>
public sealed class ScriptTests
{
    private static Script Create(
        string id = "latin",
        string name = "Latin",
        string nativeName = "Latin",
        WritingDirection direction = WritingDirection.LeftToRight)
    {
        return new Script(new ScriptId(id), name, nativeName, direction, [new Iso15924Code("Latn")]);
    }

    [Fact]
    public void Constructor_ValidArguments_ExposesEveryValue()
    {
        Script script = Create();

        Assert.Equal("latin", script.Id.Value);
        Assert.Equal("Latin", script.Name);
        Assert.Equal("Latin", script.NativeName);
        Assert.Equal(WritingDirection.LeftToRight, script.Direction);
        Assert.Equal("Latn", script.Codes.Get<Iso15924Code>().Value);
    }

    [Fact]
    public void Constructor_RightToLeftDirection_IsPreserved()
    {
        Script script = new(
            new ScriptId("hebrew"),
            "Hebrew",
            "Hebrew",
            WritingDirection.RightToLeft,
            [new Iso15924Code("Hebr")]);

        Assert.Equal(WritingDirection.RightToLeft, script.Direction);
    }

    [Fact]
    public void Constructor_NullId_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new Script(null!, "Latin", "Latin", WritingDirection.LeftToRight, [new Iso15924Code("Latn")]));
    }

    [Fact]
    public void Constructor_NullCodes_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new Script(new ScriptId("latin"), "Latin", "Latin", WritingDirection.LeftToRight, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyName_ThrowsArgumentException(string name)
    {
        Assert.Throws<ArgumentException>(() => Create(name: name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyNativeName_ThrowsArgumentException(string nativeName)
    {
        Assert.Throws<ArgumentException>(() => Create(nativeName: nativeName));
    }

    [Fact]
    public void Constructor_EmptyCodes_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new Script(new ScriptId("latin"), "Latin", "Latin", WritingDirection.LeftToRight, []));

        Assert.Contains("cannot be empty", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Equals_SameId_ReturnsTrue()
    {
        Script left = Create(name: "Latin", nativeName: "Latin");
        Script right = Create(name: "Other name", nativeName: "Other native name");

        Assert.True(left.Equals(right));
        Assert.True(left == right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentId_ReturnsFalse()
    {
        Script left = Create(id: "latin");
        Script right = Create(id: "cyrillic");

        Assert.False(left.Equals(right));
        Assert.True(left != right);
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        Script script = Create();

        Assert.False(script.Equals(null));
        Assert.False(script.Equals((object?)null));
    }

    [Fact]
    public void ToString_ReturnsName()
    {
        Assert.Equal("Latin", Create().ToString());
    }
}
