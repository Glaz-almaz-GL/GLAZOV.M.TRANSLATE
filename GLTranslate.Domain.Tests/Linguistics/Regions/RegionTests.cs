using GLTranslate.Abstractions.Linguistics.Regions;
using GLTranslate.Domain.Linguistics.Regions;
using GLTranslate.Domain.Linguistics.Regions.Codes;

namespace GLTranslate.Domain.Tests.Linguistics.Regions;

/// <summary>
/// Verifies construction-time validation and equality of the
/// <see cref="Region"/> entity.
/// </summary>
public sealed class RegionTests
{
    private static Region Create(string id = "russia", string name = "Russia")
    {
        return new Region(
            new RegionId(id),
            name,
            [new Iso3166Alpha2Code("RU"), new Iso3166Alpha3Code("RUS"), new Iso3166NumericCode("643")]);
    }

    [Fact]
    public void Constructor_ValidArguments_ExposesEveryValue()
    {
        Region region = Create();

        Assert.Equal("russia", region.Id.Value);
        Assert.Equal("Russia", region.Name);
        Assert.Equal("RU", region.Codes.Get<Iso3166Alpha2Code>().Value);
        Assert.Equal("RUS", region.Codes.Get<Iso3166Alpha3Code>().Value);
        Assert.Equal("643", region.Codes.Get<Iso3166NumericCode>().Value);
    }

    [Fact]
    public void Constructor_NullId_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new Region(null!, "Russia", [new Iso3166Alpha2Code("RU")]));
    }

    [Fact]
    public void Constructor_NullCodes_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new Region(new RegionId("russia"), "Russia", null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyName_ThrowsArgumentException(string name)
    {
        Assert.Throws<ArgumentException>(() => Create(name: name));
    }

    [Fact]
    public void Constructor_EmptyCodes_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new Region(new RegionId("russia"), "Russia", []));

        Assert.Contains("at least one region code", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Equals_SameId_ReturnsTrue()
    {
        Region left = Create(name: "Russia");
        Region right = Create(name: "Other name");

        Assert.True(left.Equals(right));
        Assert.True(left == right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentId_ReturnsFalse()
    {
        Assert.False(Create(id: "russia").Equals(Create(id: "france")));
        Assert.True(Create(id: "russia") != Create(id: "france"));
    }

    [Fact]
    public void ToString_ReturnsName()
    {
        Assert.Equal("Russia", Create().ToString());
    }
}
