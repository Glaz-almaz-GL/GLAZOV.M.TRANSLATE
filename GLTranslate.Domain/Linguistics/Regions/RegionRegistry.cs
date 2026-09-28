using GLTranslate.Abstractions.Linguistics.Regions;
using GLTranslate.Domain.Linguistics.Regions.Generated;
using GLTranslate.Domain.Registries;

namespace GLTranslate.Domain.Linguistics.Regions;

/// <summary>
/// Represents an immutable registry of region entities.
/// </summary>
/// <remarks>
/// Provides read-only lookup of regions by identifier.
///
/// The registry is immutable and thread-safe.
/// </remarks>
public sealed partial class RegionRegistry(IEnumerable<Region> regions) : ImmutableRegistry<Region, RegionId>(regions)
{
    /// <summary>
    /// Gets the default registry, populated from ISO 3166-1 region data.
    /// </summary>
    public static readonly RegionRegistry Default = new(RegionRegistryData.All);
}
