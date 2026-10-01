using GLTranslate.Abstractions.Common;
using GLTranslate.Abstractions.Interfaces;
using GLTranslate.Abstractions.Linguistics.Scripts;
using System.Diagnostics;

namespace GLTranslate.Domain.Linguistics.Scripts;

/// <summary>
/// Represents a writing system supported by GLTranslate.
/// </summary>
/// <remarks>
/// <para>
/// A writing system represents a script independently from the languages
/// written in it and from translation providers.
/// </para>
/// <para>
/// Instances of <see cref="Script"/> are immutable and thread-safe.
/// </para>
/// </remarks>
[DebuggerDisplay("{Id,nq} ({Name})")]
public sealed class Script :
    IIdentifiable<ScriptId>,
    IEquatable<Script>
{
    #region Properties

    /// <summary>
    /// Gets the unique identifier of the writing system.
    /// </summary>
    public ScriptId Id { get; }

    /// <summary>
    /// Gets the English name of the writing system.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the native name of the writing system.
    /// </summary>
    public string NativeName { get; }

    /// <summary>
    /// Gets the direction in which the writing system is written.
    /// </summary>
    public WritingDirection Direction { get; }

    /// <summary>
    /// Gets the set of all code representations associated with this writing system.
    /// </summary>
    public CodeSet<ScriptCode> Codes { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="Script"/> class.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the writing system.
    /// </param>
    /// <param name="name">
    /// The English name of the writing system.
    /// </param>
    /// <param name="nativeName">
    /// The native name of the writing system.
    /// </param>
    /// <param name="direction">
    /// The direction in which the writing system is written.
    /// </param>
    /// <param name="codes">
    /// The writing system code representations.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="id"/> or <paramref name="codes"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="nativeName"/> is empty or consists only of white-space characters,
    /// or when <paramref name="codes"/> contains null or duplicate elements.
    /// </exception>
    internal Script(
        ScriptId id,
        string name,
        string nativeName,
        WritingDirection direction,
        IEnumerable<ScriptCode> codes)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(nativeName);
        ArgumentNullException.ThrowIfNull(codes);

        if (!codes.Any())
        {
            throw new ArgumentException("The collection of codes cannot be empty.");
        }

        Id = id;
        Name = name;
        NativeName = nativeName;
        Direction = direction;
        Codes = new CodeSet<ScriptCode>(codes);
    }

    #endregion

    #region Equality Members

    /// <inheritdoc />
    public bool Equals(Script? other)
    {
        return ReferenceEquals(this, other) || (other is not null && Id.Equals(other.Id));
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Script other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    /// <summary>
    /// Determines whether two <see cref="Script"/> instances represent the same writing system.
    /// </summary>
    public static bool operator ==(Script? left, Script? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Determines whether two <see cref="Script"/> instances represent different writing systems.
    /// </summary>
    public static bool operator !=(Script? left, Script? right)
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