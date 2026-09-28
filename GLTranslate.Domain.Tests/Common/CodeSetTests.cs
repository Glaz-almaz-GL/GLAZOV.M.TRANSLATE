using GLTranslate.Abstractions.Common;
using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Linguistics.Regions;
using GLTranslate.Domain.Linguistics.Languages.Codes;
using GLTranslate.Domain.Linguistics.Regions.Codes;

namespace GLTranslate.Domain.Tests.Common;

/// <summary>
/// Verifies the guarantees of <see cref="CodeSet{TCode}"/>: one code per type,
/// no nulls, and predictable lookup behaviour.
/// </summary>
public sealed class CodeSetTests
{
    private static CodeSet<LanguageCode> CreateSet()
    {
        return new CodeSet<LanguageCode>([new Iso6391Code("ru"), new Iso6392Code("rus")]);
    }

    [Fact]
    public void Constructor_NullCollection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new CodeSet<LanguageCode>(null!));
    }

    [Fact]
    public void Constructor_NullElement_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new CodeSet<LanguageCode>([new Iso6391Code("ru"), null!]));
    }

    [Fact]
    public void Constructor_DuplicateCodeType_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new CodeSet<LanguageCode>([new Iso6391Code("ru"), new Iso6391Code("en")]));

        Assert.Contains("Duplicate code types", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_EmptyCollection_CreatesEmptySet()
    {
        CodeSet<LanguageCode> set = new([]);

        Assert.Empty(set);
    }

    [Fact]
    public void Get_ExistingType_ReturnsCode()
    {
        CodeSet<LanguageCode> set = CreateSet();

        Assert.Equal("ru", set.Get<Iso6391Code>().Value);
        Assert.Equal("rus", set.Get(typeof(Iso6392Code)).Value);
    }

    [Fact]
    public void Get_MissingType_ThrowsInvalidOperationException()
    {
        CodeSet<LanguageCode> set = CreateSet();

        Assert.Throws<InvalidOperationException>(() => set.Get<Iso6393Code>());
        Assert.Throws<InvalidOperationException>(() => set.Get(typeof(Iso6393Code)));
    }

    [Fact]
    public void Get_NullType_ThrowsArgumentNullException()
    {
        CodeSet<LanguageCode> set = CreateSet();

        Assert.Throws<ArgumentNullException>(() => set.Get(null!));
    }

    [Fact]
    public void Get_IncompatibleType_ThrowsArgumentException()
    {
        CodeSet<LanguageCode> set = CreateSet();

        Assert.Throws<ArgumentException>(() => set.Get(typeof(Iso3166Alpha2Code)));
    }

    [Fact]
    public void TryGetValue_ExistingType_ReturnsTrue()
    {
        CodeSet<LanguageCode> set = CreateSet();

        Assert.True(set.TryGetValue(out Iso6391Code? code));
        Assert.Equal("ru", code.Value);

        Assert.True(set.TryGetValue(typeof(Iso6392Code), out LanguageCode? byType));
        Assert.Equal("rus", byType.Value);
    }

    [Fact]
    public void TryGetValue_MissingType_ReturnsFalse()
    {
        CodeSet<LanguageCode> set = CreateSet();

        Assert.False(set.TryGetValue(out Iso6393Code? code));
        Assert.Null(code);
    }

    [Fact]
    public void TryGetValue_IncompatibleType_ReturnsFalse()
    {
        CodeSet<LanguageCode> set = CreateSet();

        Assert.False(set.TryGetValue(typeof(Iso3166Alpha2Code), out LanguageCode? code));
        Assert.Null(code);
    }

    [Fact]
    public void TryGetValue_NullType_ThrowsArgumentNullException()
    {
        CodeSet<LanguageCode> set = CreateSet();

        Assert.Throws<ArgumentNullException>(() => set.TryGetValue(null!, out LanguageCode? _));
    }

    [Fact]
    public void Contains_ReflectsSetContent()
    {
        CodeSet<LanguageCode> set = CreateSet();

        Assert.True(set.Contains<Iso6391Code>());
        Assert.False(set.Contains<Iso6393Code>());
        Assert.True(set.Contains(typeof(Iso6392Code)));
        Assert.False(set.Contains(typeof(Iso3166Alpha2Code)));
    }

    [Fact]
    public void Contains_NullType_ThrowsArgumentNullException()
    {
        CodeSet<LanguageCode> set = CreateSet();

        Assert.Throws<ArgumentNullException>(() => set.Contains(null!));
    }

    [Fact]
    public void Indexer_PreservesOrder()
    {
        CodeSet<LanguageCode> set = CreateSet();

        Assert.Equal(2, set.Count);
        Assert.Equal("ru", set[0].Value);
        Assert.Equal("rus", set[1].Value);
    }

    [Fact]
    public void Indexer_OutOfRange_ThrowsArgumentOutOfRangeException()
    {
        CodeSet<LanguageCode> set = CreateSet();

        Assert.Throws<ArgumentOutOfRangeException>(() => set[2]);
        Assert.Throws<ArgumentOutOfRangeException>(() => set[-1]);
    }

    [Fact]
    public void Enumeration_YieldsEveryCode()
    {
        CodeSet<LanguageCode> set = CreateSet();

        List<string> values = [];

        foreach (LanguageCode code in set)
        {
            values.Add(code.Value);
        }

        Assert.Equal(["ru", "rus"], values);
    }
}
