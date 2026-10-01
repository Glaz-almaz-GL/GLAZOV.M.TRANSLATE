using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Scripts;
using GLAZOV.M.TRANSLATE.Domain.Linguistics.Scripts;
using GLAZOV.M.TRANSLATE.Domain.Linguistics.Scripts.Codes;

namespace GLAZOV.M.TRANSLATE.Domain.Tests.Linguistics.Scripts;

/// <summary>
/// Verifies the lookup contract of <see cref="ScriptRegistry"/> and the
/// consistency of the generated script data.
/// </summary>
public sealed class ScriptRegistryTests
{
    private static ScriptRegistry Registry => ScriptRegistry.Default;

    [Fact]
    public void Get_KnownId_ReturnsScript()
    {
        Script script = Registry.Get(new ScriptId("latin"));

        Assert.Equal("Latin", script.Name);
    }

    [Fact]
    public void Get_UnknownId_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() => Registry.Get(new ScriptId("does_not_exist")));
    }

    [Fact]
    public void Get_NullId_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Registry.Get(null!));
    }

    [Fact]
    public void TryGet_KnownId_ReturnsTrue()
    {
        Assert.True(Registry.TryGet(new ScriptId("cyrillic"), out Script? script));
        Assert.Equal("Cyrillic", script.Name);
    }

    [Fact]
    public void TryGet_UnknownId_ReturnsFalse()
    {
        Assert.False(Registry.TryGet(new ScriptId("does_not_exist"), out Script? script));
        Assert.Null(script);
    }

    [Fact]
    public void Contains_ReflectsRegistryContent()
    {
        Assert.True(Registry.Contains(new ScriptId("latin")));
        Assert.False(Registry.Contains(new ScriptId("does_not_exist")));
    }

    [Fact]
    public void GeneratedProperty_ReturnsSameInstanceAsGet()
    {
        Assert.Same(Registry.Get(new ScriptId("latin")), Registry.Latin);
    }

    [Fact]
    public void All_ContainsEveryScriptExactlyOnce()
    {
        Assert.NotEmpty(Registry.All);
        Assert.Equal(Registry.All.Count, Registry.All.Select(x => x.Id).Distinct().Count());
    }

    [Fact]
    public void All_EveryScriptHasIso15924Code()
    {
        foreach (Script script in Registry.All)
        {
            Assert.True(script.Codes.Contains<Iso15924Code>());
            Assert.Equal(4, script.Codes.Get<Iso15924Code>().Value.Length);
        }
    }

    [Theory]
    [InlineData("Arab")]
    [InlineData("Hebr")]
    [InlineData("Thaa")]
    public void RightToLeftScripts_AreMarkedRightToLeft(string iso15924)
    {
        Script script = Registry.All.Single(x => x.Codes.Get<Iso15924Code>().Value == iso15924);

        Assert.Equal(WritingDirection.RightToLeft, script.Direction);
    }

    [Theory]
    [InlineData("Latn")]
    [InlineData("Cyrl")]
    [InlineData("Grek")]
    [InlineData("Hans")]
    public void LeftToRightScripts_AreMarkedLeftToRight(string iso15924)
    {
        Script script = Registry.All.Single(x => x.Codes.Get<Iso15924Code>().Value == iso15924);

        Assert.Equal(WritingDirection.LeftToRight, script.Direction);
    }
}
