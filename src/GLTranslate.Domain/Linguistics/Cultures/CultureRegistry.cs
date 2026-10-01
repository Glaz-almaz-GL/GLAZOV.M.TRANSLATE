using GLTranslate.Abstractions.Linguistics.Cultures;
using GLTranslate.Domain.Linguistics.Cultures.Generated;
using GLTranslate.Domain.Registries;

namespace GLTranslate.Domain.Linguistics.Cultures;

/// <summary>
/// Represents an immutable registry of culture entities.
/// </summary>
/// <remarks>
/// Provides read-only lookup of cultures by their BCP 47 identifier.
///
/// The registry is immutable and thread-safe.
/// </remarks>
public sealed class CultureRegistry(IEnumerable<Culture> cultures) : ImmutableRegistry<Culture, CultureId>(cultures)
{
    /// <summary>
    /// Gets the default registry, populated from BCP 47 culture data.
    /// </summary>
    public static readonly CultureRegistry Default = new(CultureRegistryData.All);
}
