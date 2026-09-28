using GLTranslate.Abstractions.Linguistics.Regions;
using GLTranslate.Domain.Linguistics.Regions.Codes;
using GLTranslate.Domain.Linguistics.Regions.Generated;
using GLTranslate.Domain.Registries;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace GLTranslate.Domain.Linguistics.Regions;

/// <summary>
/// Represents an immutable registry of region entities.
/// </summary>
/// <remarks>
/// Provides read-only lookup of regions by identifier and by
/// ISO 3166-1 alpha-2 code.
///
/// The registry is immutable and thread-safe.
/// </remarks>
public sealed partial class RegionRegistry : ImmutableRegistry<Region, RegionId>
{
    private readonly ImmutableDictionary<string, Region> _byAlpha2;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegionRegistry"/> class.
    /// </summary>
    /// <param name="regions">
    /// The regions contained in the registry. A region without an
    /// ISO 3166-1 alpha-2 code takes no part in the alpha-2 lookup.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="regions"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when duplicate region identifiers are found, or when two
    /// regions carry the same ISO 3166-1 alpha-2 code.
    /// </exception>
    public RegionRegistry(IEnumerable<Region> regions)
        : base(regions)
    {
        ImmutableDictionary<string, Region>.Builder builder =
            ImmutableDictionary.CreateBuilder<string, Region>(StringComparer.Ordinal);

        foreach (Region region in All)
        {
            if (!region.Codes.TryGetValue(out Iso3166Alpha2Code? code))
            {
                continue;
            }

            if (builder.ContainsKey(code.Value))
            {
                throw new ArgumentException(
                    $"Duplicate ISO 3166-1 alpha-2 codes are not allowed. Code '{code.Value}' is duplicated.",
                    nameof(regions));
            }

            builder.Add(code.Value, region);
        }

        _byAlpha2 = builder.ToImmutable();
    }

    /// <summary>
    /// Gets the default registry, populated from ISO 3166-1 region data.
    /// </summary>
    public static readonly RegionRegistry Default = new(RegionRegistryData.All);

    /// <summary>
    /// Gets the region carrying the specified ISO 3166-1 alpha-2 code.
    /// </summary>
    /// <param name="alpha2">
    /// The ISO 3166-1 alpha-2 code of the region. The value is normalized
    /// by <see cref="Iso3166Alpha2Code"/>, so case and surrounding
    /// white space do not matter.
    /// </param>
    /// <returns>
    /// The region carrying the specified code.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="alpha2"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="alpha2"/> is not a well-formed
    /// ISO 3166-1 alpha-2 code.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no region carries the specified code.
    /// </exception>
    public Region GetByAlpha2(string alpha2)
    {
        if (!TryGetByAlpha2(alpha2, out Region? region))
        {
            // Unknown region code
            throw new KeyNotFoundException($"'{alpha2}' is not a known ISO 3166-1 region.");
        }

        return region;
    }

    /// <summary>
    /// Attempts to get the region carrying the specified ISO 3166-1 alpha-2 code.
    /// </summary>
    /// <param name="alpha2">
    /// The ISO 3166-1 alpha-2 code of the region. The value is normalized
    /// by <see cref="Iso3166Alpha2Code"/>, so case and surrounding
    /// white space do not matter.
    /// </param>
    /// <param name="region">
    /// When this method returns <see langword="true"/>, contains the region
    /// carrying the specified code. Otherwise, contains <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when a region carries the code;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="alpha2"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="alpha2"/> is not a well-formed
    /// ISO 3166-1 alpha-2 code.
    /// </exception>
    public bool TryGetByAlpha2(string alpha2, [NotNullWhen(true)] out Region? region)
    {
        Iso3166Alpha2Code code = new(alpha2);

        return _byAlpha2.TryGetValue(code.Value, out region);
    }
}
