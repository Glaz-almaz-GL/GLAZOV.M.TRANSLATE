using GLAZOV.M.TRANSLATE.Abstractions.Common;
using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Scripts;
using GLAZOV.M.TRANSLATE.Domain.Linguistics.Scripts;

namespace GLAZOV.M.TRANSLATE.Domain.Tests.Common;

/// <summary>
/// Verifies the guarantees of <see cref="EntitySet{TEntity, TId}"/>:
/// no nulls, no duplicates, stable order and allocation-free enumeration.
/// </summary>
public sealed class EntitySetTests
{
    private static Script Latin => ScriptRegistry.Default.Latin;

    private static Script Cyrillic => ScriptRegistry.Default.Cyrillic;

    [Fact]
    public void Constructor_NullCollection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new EntitySet<Script, ScriptId>(null!));
    }

    [Fact]
    public void Constructor_NullElement_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new EntitySet<Script, ScriptId>([Latin, null!]));
    }

    [Fact]
    public void Constructor_DuplicateEntity_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new EntitySet<Script, ScriptId>([Latin, Latin]));

        Assert.Contains("Duplicate entities", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_EmptyCollection_CreatesEmptySet()
    {
        EntitySet<Script, ScriptId> set = new([]);

        Assert.Empty(set);
    }

    [Fact]
    public void Indexer_PreservesOrder()
    {
        EntitySet<Script, ScriptId> set = new([Latin, Cyrillic]);

        Assert.Equal(2, set.Count);
        Assert.Equal(Latin, set[0]);
        Assert.Equal(Cyrillic, set[1]);
    }

    [Fact]
    public void Indexer_OutOfRange_ThrowsArgumentOutOfRangeException()
    {
        EntitySet<Script, ScriptId> set = new([Latin]);

        Assert.Throws<ArgumentOutOfRangeException>(() => set[1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => set[-1]);
    }

    [Fact]
    public void Contains_ReflectsSetContent()
    {
        EntitySet<Script, ScriptId> set = new([Latin]);

        Assert.True(set.Contains(Latin));
        Assert.False(set.Contains(Cyrillic));
    }

    [Fact]
    public void Contains_Null_ThrowsArgumentNullException()
    {
        EntitySet<Script, ScriptId> set = new([Latin]);

        Assert.Throws<ArgumentNullException>(() => set.Contains(null!));
    }

    [Fact]
    public void Enumeration_YieldsEveryEntity()
    {
        EntitySet<Script, ScriptId> set = new([Latin, Cyrillic]);

        List<Script> scripts = [];

        foreach (Script script in set)
        {
            scripts.Add(script);
        }

        Assert.Equal([Latin, Cyrillic], scripts);
    }
}
