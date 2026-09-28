using GLTranslate.Abstractions.Linguistics.Cultures;
using GLTranslate.Domain.Linguistics.Cultures;
using GLTranslate.Domain.Linguistics.Cultures.Codes;
using GLTranslate.Domain.Linguistics.Languages;
using GLTranslate.Domain.Linguistics.Regions;

namespace GLTranslate.Domain.Tests.Linguistics.Cultures;

/// <summary>
/// Verifies the lookup contract of <see cref="CultureRegistry"/> and the
/// consistency of the generated culture data.
/// </summary>
public sealed class CultureRegistryTests
{
    private static CultureRegistry Registry => CultureRegistry.Default;

    [Fact]
    public void Get_KnownId_ReturnsCulture()
    {
        Culture culture = Registry.Get(new CultureId("ru-RU"));

        Assert.Equal("russian", culture.Language.Id.Value);
        Assert.Equal("russia", culture.Region!.Id.Value);
    }

    [Fact]
    public void Get_UnknownId_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() => Registry.Get(new CultureId("zz-ZZ")));
    }

    [Fact]
    public void Get_NullId_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Registry.Get(null!));
    }

    [Fact]
    public void All_ContainsEveryCultureExactlyOnce()
    {
        Assert.NotEmpty(Registry.All);
        Assert.Equal(Registry.All.Count, Registry.All.Select(x => x.Id).Distinct().Count());
    }

    [Fact]
    public void All_EveryCultureCarriesItsBcp47Code()
    {
        foreach (Culture culture in Registry.All)
        {
            Assert.Equal(culture.Id.Value, culture.Codes.Get<Bcp47Code>().Value);
        }
    }

    [Fact]
    public void All_LanguagesAndRegionsComeFromTheirRegistries()
    {
        foreach (Culture culture in Registry.All)
        {
            Assert.True(LanguageRegistry.Default.Contains(culture.Language.Id));

            if (culture.Region is not null)
            {
                Assert.True(RegionRegistry.Default.Contains(culture.Region.Id));
            }
        }
    }

    [Fact]
    public void All_ScriptIsAlwaysSupportedByItsLanguage()
    {
        foreach (Culture culture in Registry.All)
        {
            if (culture.Script is not null)
            {
                Assert.True(culture.Language.Scripts.Contains(culture.Script));
            }
        }
    }
}
