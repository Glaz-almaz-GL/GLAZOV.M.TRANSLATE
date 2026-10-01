namespace GLTranslate.Abstractions.Interfaces;

/// <summary>
/// Represents a textual code used to identify a domain entity
/// within a specific coding system.
/// </summary>
/// <remarks>
/// <para>
/// A code provides a standardized or provider-specific textual
/// representation of an entity.
/// </para>
/// <para>
/// Different code systems may represent the same entity using
/// different values.
/// </para>
/// </remarks>
public interface ICode
{
    /// <summary>
    /// Gets the textual representation of the code.
    /// </summary>
    string Value { get; }
}