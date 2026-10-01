using GLTranslate.Abstractions.Linguistics.Regions;
using GLTranslate.Domain.Linguistics.Regions;
using GLTranslate.Domain.Linguistics.Regions.Codes;

namespace GLTranslate.Domain.Tests.Linguistics.Regions;

/// <summary>
/// Verifies the lookup contract of <see cref="RegionRegistry"/> and the
/// consistency of the generated region data.
/// </summary>
public sealed class RegionRegistryTests
{
    private static RegionRegistry Registry => RegionRegistry.Default;

    [Fact]
    public void Get_KnownId_ReturnsRegion()
    {
        Assert.Equal("Russia", Registry.Get(new RegionId("russia")).Name);
    }

    [Fact]
    public void Get_UnknownId_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() => Registry.Get(new RegionId("does_not_exist")));
    }

    [Fact]
    public void Get_NullId_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Registry.Get(null!));
    }

    [Fact]
    public void Contains_ReflectsRegistryContent()
    {
        Assert.True(Registry.Contains(new RegionId("russia")));
        Assert.False(Registry.Contains(new RegionId("does_not_exist")));
    }

    [Fact]
    public void All_ContainsEveryRegionExactlyOnce()
    {
        Assert.NotEmpty(Registry.All);
        Assert.Equal(Registry.All.Count, Registry.All.Select(x => x.Id).Distinct().Count());
    }

    [Fact]
    public void All_EveryRegionHasAlpha2AndAlpha3Codes()
    {
        foreach (Region region in Registry.All)
        {
            Assert.Equal(2, region.Codes.Get<Iso3166Alpha2Code>().Value.Length);
            Assert.Equal(3, region.Codes.Get<Iso3166Alpha3Code>().Value.Length);
        }
    }

    [Fact]
    public void All_Alpha2CodesAreUnique()
    {
        int distinct = Registry.All
            .Select(x => x.Codes.Get<Iso3166Alpha2Code>().Value)
            .Distinct(StringComparer.Ordinal)
            .Count();

        Assert.Equal(Registry.All.Count, distinct);
    }

    [Fact]
    public void Kosovo_HasNoNumericCode()
    {
        Region kosovo = Registry.All.Single(x => x.Codes.Get<Iso3166Alpha2Code>().Value == "XK");

        Assert.False(kosovo.Codes.Contains<Iso3166NumericCode>());
    }
}
