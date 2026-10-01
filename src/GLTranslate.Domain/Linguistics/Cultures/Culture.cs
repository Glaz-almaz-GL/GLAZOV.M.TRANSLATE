using GLTranslate.Abstractions.Common;
using GLTranslate.Abstractions.Interfaces;
using GLTranslate.Abstractions.Linguistics.Cultures;
using GLTranslate.Domain.Linguistics.Languages;
using GLTranslate.Domain.Linguistics.Regions;
using GLTranslate.Domain.Linguistics.Scripts;
using System.Diagnostics;

namespace GLTranslate.Domain.Linguistics.Cultures;

/// <summary>
/// Represents a linguistic culture supported by GLTranslate.
/// </summary>
/// <remarks>
/// <para>
/// A culture binds a language to the region and the writing system it is
/// used with. Both are optional: a culture may name a language alone.
/// </para>
/// <para>
/// Instances of <see cref="Culture"/> are immutable and thread-safe.
/// </para>
/// </remarks>
[DebuggerDisplay("{Id,nq}")]
public sealed class Culture :
    IIdentifiable<CultureId>,
    IEquatable<Culture>
{
    #region Properties

    /// <summary>
    /// Gets the unique identifier of the culture within GLTranslate.
    /// </summary>
    public CultureId Id { get; }

    /// <summary>
    /// Gets the language represented by the culture.
    /// </summary>
    public Language Language { get; }

    /// <summary>
    /// Gets the region associated with the culture.
    /// </summary>
    public Region? Region { get; }

    /// <summary>
    /// Gets the writing system associated with the culture.
    /// </summary>
    public Script? Script { get; }

    /// <summary>
    /// Gets the set of all code representations associated with this culture.
    /// </summary>
    public CodeSet<CultureCode> Codes { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="Culture"/> class.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the culture within GLTranslate.
    /// </param>
    /// <param name="language">
    /// The language represented by the culture.
    /// </param>
    /// <param name="region">
    /// The region associated with the culture, or <see langword="null"/>
    /// when the culture is not bound to a region.
    /// </param>
    /// <param name="script">
    /// The writing system associated with the culture, or <see langword="null"/>
    /// when the culture does not name one. When specified, it must be one of
    /// the writing systems of <paramref name="language"/>.
    /// </param>
    /// <param name="codes">
    /// The culture code representations.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="id"/>, <paramref name="language"/>, or <paramref name="codes"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the specified <paramref name="script"/> is not supported by the specified
    /// <paramref name="language"/>, or when <paramref name="codes"/> contains null or duplicate elements.
    /// </exception>
    internal Culture(
        CultureId id,
        Language language,
        Region? region,
        Script? script,
        IEnumerable<CultureCode> codes)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(language);
        ArgumentNullException.ThrowIfNull(codes);

        if (script is not null && !language.Scripts.Contains(script))
        {
            throw new ArgumentException($"The script '{script.Name}' is not supported by language '{language.Name}'.", nameof(script));
        }

        Id = id;
        Language = language;
        Region = region;
        Script = script;

        Codes = new CodeSet<CultureCode>(codes);
    }

    #endregion

    #region Equality Members

    /// <inheritdoc />
    public bool Equals(Culture? other)
    {
        return ReferenceEquals(this, other) || (other is not null && Id.Equals(other.Id));
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Culture other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    /// <summary>
    /// Determines whether two <see cref="Culture"/> instances represent the same culture.
    /// </summary>
    public static bool operator ==(Culture? left, Culture? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Determines whether two <see cref="Culture"/> instances represent different cultures.
    /// </summary>
    public static bool operator !=(Culture? left, Culture? right)
    {
        return !Equals(left, right);
    }

    #endregion

    #region Object Overrides

    /// <inheritdoc />
    public override string ToString()
    {
        return Id.ToString();
    }

    #endregion
}