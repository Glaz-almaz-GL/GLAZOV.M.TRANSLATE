using GLAZOV.M.TRANSLATE.Abstractions.Common;
using GLAZOV.M.TRANSLATE.Abstractions.Interfaces;
using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Scripts;
using GLAZOV.M.TRANSLATE.Domain.Linguistics.Scripts;
using System.Diagnostics;

namespace GLAZOV.M.TRANSLATE.Domain.Linguistics.Languages;

/// <summary>
/// Represents a human language supported by GLAZOV.M.TRANSLATE.
/// </summary>
/// <remarks>
/// <para>
/// A language represents a natural language independently from
/// localization cultures and translation providers.
/// </para>
/// <para>
/// Instances of <see cref="Language"/> are immutable and thread-safe.
/// </para>
/// </remarks>
[DebuggerDisplay("{Id,nq} ({Name})")]
public sealed class Language :
    IIdentifiable<LanguageId>,
    IEquatable<Language>
{
    #region Properties

    /// <summary>
    /// Gets the unique language identifier.
    /// </summary>
    public LanguageId Id { get; }

    /// <summary>
    /// Gets the English language name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the native language name.
    /// </summary>
    public string NativeName { get; }

    /// <summary>
    /// Gets the set of all code representations associated with this language.
    /// </summary>
    public CodeSet<LanguageCode> Codes { get; }

    /// <summary>
    /// Gets the writing systems the language is written in.
    /// </summary>
    /// <remarks>
    /// The order is meaningful: the first writing system is the primary one
    /// for the language. A language written in several scripts may be
    /// written in several directions, so the writing direction is read from
    /// the writing system rather than from the language.
    /// </remarks>
    public EntitySet<Script, ScriptId> Scripts { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="Language"/> class.
    /// </summary>
    /// <param name="id">
    /// The unique language identifier.
    /// </param>
    /// <param name="name">
    /// The English language name.
    /// </param>
    /// <param name="nativeName">
    /// The native language name.
    /// </param>
    /// <param name="scripts">
    /// The supported writing systems.
    /// </param>
    /// <param name="codes">
    /// The language code representations.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when one of the required arguments is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or
    /// <paramref name="nativeName"/> is empty or consists only of
    /// white-space characters.
    /// </exception>
    internal Language(
        LanguageId id,
        string name,
        string nativeName,
        IEnumerable<Script> scripts,
        IEnumerable<LanguageCode> codes)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(nativeName);
        ArgumentNullException.ThrowIfNull(scripts);
        ArgumentNullException.ThrowIfNull(codes);

        Id = id;
        Name = name;
        NativeName = nativeName;

        Scripts = new EntitySet<Script, ScriptId>(scripts);
        Codes = new CodeSet<LanguageCode>(codes);
    }

    #endregion

    #region Equality

    /// <inheritdoc />
    public bool Equals(Language? other)
    {
        return ReferenceEquals(this, other) || (other is not null && Id.Equals(other.Id));
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Language other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    /// <summary>
    /// Determines whether two <see cref="Language"/> instances represent
    /// the same language.
    /// </summary>
    public static bool operator ==(Language? left, Language? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Determines whether two <see cref="Language"/> instances represent
    /// different languages.
    /// </summary>
    public static bool operator !=(Language? left, Language? right)
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