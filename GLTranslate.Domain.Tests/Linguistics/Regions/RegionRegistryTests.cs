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

    [Theory]
    [InlineData("RU")]
    [InlineData("ru")]
    [InlineData("  ru  ")]
    public void GetByAlpha2_KnownCode_IsCaseAndWhiteSpaceInsensitive(string alpha2)
    {
        Region region = Registry.GetByAlpha2(alpha2);

        Assert.Equal("RU", region.Codes.Get<Iso3166Alpha2Code>().Value);
    }

    [Fact]
    public void GetByAlpha2_UnknownCode_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() => Registry.GetByAlpha2("ZZ"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("RUS")]
    [InlineData("Р")]
    public void GetByAlpha2_MalformedCode_ThrowsArgumentException(string alpha2)
    {
        Assert.Throws<ArgumentException>(() => Registry.GetByAlpha2(alpha2));
    }

    [Fact]
    public void GetByAlpha2_NullCode_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Registry.GetByAlpha2(null!));
    }

    [Fact]
    public void TryGetByAlpha2_KnownCode_ReturnsTrue()
    {
        Assert.True(Registry.TryGetByAlpha2("US", out Region? region));
        Assert.Equal("US", region.Codes.Get<Iso3166Alpha2Code>().Value);
    }

    [Fact]
    public void TryGetByAlpha2_UnknownCode_ReturnsFalse()
    {
        Assert.False(Registry.TryGetByAlpha2("ZZ", out Region? region));
        Assert.Null(region);
    }

    [Fact]
    public void TryGetByAlpha2_ReturnsTheSameInstanceAsGet()
    {
        Assert.True(Registry.TryGetByAlpha2("RU", out Region? region));
        Assert.Same(Registry.Get(region!.Id), region);
    }

    [Fact]
    public void Constructor_DuplicateAlpha2Code_ThrowsArgumentException()
    {
        Region russia = Registry.GetByAlpha2("RU");
        Region twin = new(new RegionId("russia_twin"), "Russia twin", [new Iso3166Alpha2Code("RU")]);

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new RegionRegistry([russia, twin]));

        Assert.Contains("Duplicate ISO 3166-1 alpha-2 codes", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_RegionWithoutAlpha2Code_IsNotFoundByAlpha2()
    {
        Region numericOnly = new(new RegionId("numeric_only"), "Numeric only", [new Iso3166NumericCode("999")]);

        RegionRegistry registry = new([numericOnly]);

        Assert.True(registry.Contains(numericOnly.Id));
        Assert.False(registry.TryGetByAlpha2("RU", out Region? region));
        Assert.Null(region);
    }
}
