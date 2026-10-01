using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Cultures;
using GLAZOV.M.TRANSLATE.Domain.Linguistics.Cultures;
using GLAZOV.M.TRANSLATE.Domain.Linguistics.Cultures.Codes;
using GLAZOV.M.TRANSLATE.Domain.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Domain.Linguistics.Regions;
using GLAZOV.M.TRANSLATE.Domain.Linguistics.Scripts;

namespace GLAZOV.M.TRANSLATE.Domain.Tests.Linguistics.Cultures;

/// <summary>
/// Verifies construction-time validation and equality of the
/// <see cref="Culture"/> entity, including the script compatibility rule.
/// </summary>
public sealed class CultureTests
{
    private static Language Russian => LanguageRegistry.Default.Get(new("russian"));

    private static Region Russia => RegionRegistry.Default.Get(new("russia"));

    private static Culture Create(
        string id = "ru-RU",
        Region? region = null,
        Script? script = null)
    {
        return new Culture(new CultureId(id), Russian, region, script, [new Bcp47Code(id)]);
    }

    [Fact]
    public void Constructor_ValidArguments_ExposesEveryValue()
    {
        Culture culture = Create(region: Russia, script: ScriptRegistry.Default.Cyrillic);

        Assert.Equal("ru-RU", culture.Id.Value);
        Assert.Equal(Russian, culture.Language);
        Assert.Equal(Russia, culture.Region);
        Assert.Equal(ScriptRegistry.Default.Cyrillic, culture.Script);
        Assert.Equal("ru-RU", culture.Codes.Get<Bcp47Code>().Value);
    }

    [Fact]
    public void Constructor_NullRegionAndScript_AreAllowed()
    {
        Culture culture = Create();

        Assert.Null(culture.Region);
        Assert.Null(culture.Script);
    }

    [Fact]
    public void Constructor_ScriptNotSupportedByLanguage_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => Create(script: ScriptRegistry.Default.Hebrew));

        Assert.Contains("is not supported by language", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_NullId_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new Culture(null!, Russian, null, null, [new Bcp47Code("ru-RU")]));
    }

    [Fact]
    public void Constructor_NullLanguage_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new Culture(new CultureId("ru-RU"), null!, null, null, [new Bcp47Code("ru-RU")]));
    }

    [Fact]
    public void Constructor_NullCodes_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new Culture(new CultureId("ru-RU"), Russian, null, null, null!));
    }

    [Fact]
    public void Equals_SameId_ReturnsTrue()
    {
        Culture left = Create();
        Culture right = Create(region: Russia);

        Assert.True(left.Equals(right));
        Assert.True(left == right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentId_ReturnsFalse()
    {
        Assert.False(Create(id: "ru-RU").Equals(Create(id: "ru-BY")));
        Assert.True(Create(id: "ru-RU") != Create(id: "ru-BY"));
    }
}
