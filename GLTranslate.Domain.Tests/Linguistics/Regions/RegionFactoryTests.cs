using GLTranslate.Domain.Linguistics.Regions;
using GLTranslate.Domain.Linguistics.Regions.Codes;

namespace GLTranslate.Domain.Tests.Linguistics.Regions;

/// <summary>
/// Verifies the alpha-2 lookup performed by <see cref="RegionFactory"/>.
/// </summary>
public sealed class RegionFactoryTests
{
    [Theory]
    [InlineData("RU")]
    [InlineData("ru")]
    [InlineData("  ru  ")]
    public void Get_KnownCode_IsCaseAndWhiteSpaceInsensitive(string alpha2)
    {
        Region region = RegionFactory.Get(alpha2);

        Assert.Equal("RU", region.Codes.Get<Iso3166Alpha2Code>().Value);
    }

    [Fact]
    public void Get_UnknownCode_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() => RegionFactory.Get("ZZ"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Get_EmptyCode_ThrowsArgumentException(string alpha2)
    {
        Assert.Throws<ArgumentException>(() => RegionFactory.Get(alpha2));
    }

    [Fact]
    public void Get_NullCode_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => RegionFactory.Get(null!));
    }

    [Fact]
    public void TryGet_KnownCode_ReturnsTrue()
    {
        Assert.True(RegionFactory.TryGet("US", out Region? region));
        Assert.Equal("US", region!.Codes.Get<Iso3166Alpha2Code>().Value);
    }

    [Fact]
    public void TryGet_UnknownCode_ReturnsFalse()
    {
        Assert.False(RegionFactory.TryGet("ZZ", out Region? region));
        Assert.Null(region);
    }

    [Fact]
    public void TryGet_ReturnsSameInstanceAsRegistry()
    {
        Assert.True(RegionFactory.TryGet("RU", out Region? region));
        Assert.Same(RegionRegistry.Default.Get(region!.Id), region);
    }
}
