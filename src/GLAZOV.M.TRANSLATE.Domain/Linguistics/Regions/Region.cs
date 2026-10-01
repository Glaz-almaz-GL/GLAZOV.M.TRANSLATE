using GLAZOV.M.TRANSLATE.Abstractions.Common;
using GLAZOV.M.TRANSLATE.Abstractions.Interfaces;
using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Regions;
using System.Diagnostics;

namespace GLAZOV.M.TRANSLATE.Domain.Linguistics.Regions;

/// <summary>
/// Represents a geographic region used for localization purposes.
/// </summary>
/// <remarks>
/// Instances of <see cref="Region"/> are immutable and thread-safe.
/// </remarks>
[DebuggerDisplay("{Id,nq} ({Name})")]
public sealed class Region :
    IIdentifiable<RegionId>,
    IEquatable<Region>
{
    #region Properties

    /// <summary>
    /// Gets the unique region identifier.
    /// </summary>
    public RegionId Id { get; }

    /// <summary>
    /// Gets the English name of the region.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the set of all code representations associated with this region.
    /// </summary>
    public CodeSet<RegionCode> Codes { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="Region"/> class.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="id"/> or <paramref name="codes"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is empty or consists only of white-space characters,
    /// when <paramref name="codes"/> is empty, or when it contains null or duplicate elements.
    /// </exception>
    internal Region(
        RegionId id,
        string name,
        IEnumerable<RegionCode> codes)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(codes);

        // Validate that the code collection is not empty without enumerating it twice.
        if (!codes.Any())
        {
            throw new ArgumentException("Region must contain at least one region code.", nameof(codes));
        }

        Id = id;
        Name = name;

        Codes = new CodeSet<RegionCode>(codes);
    }

    #endregion

    #region Equality Members

    /// <inheritdoc />
    public bool Equals(Region? other)
    {
        return ReferenceEquals(this, other) || (other is not null && Id.Equals(other.Id));
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Region other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    /// <summary>
    /// Determines whether two <see cref="Region"/> instances represent the same region.
    /// </summary>
    public static bool operator ==(Region? left, Region? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Determines whether two <see cref="Region"/> instances represent different regions.
    /// </summary>
    public static bool operator !=(Region? left, Region? right)
    {
        return !Equals(left, right);
    }

    #endregion

    #region Object Overrides

    /// <inheritdoc />
    public override string ToString()
    {
        return Name;
    }

    #endregion
}