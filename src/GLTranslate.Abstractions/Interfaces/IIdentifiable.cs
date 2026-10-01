namespace GLTranslate.Abstractions.Interfaces;

/// <summary>
/// Represents an entity that has a unique identifier.
/// </summary>
/// <typeparam name="TId">
/// The type of the entity identifier.
/// </typeparam>
public interface IIdentifiable<out TId> where TId : notnull
{
    /// <summary>
    /// Gets the unique identifier of the entity.
    /// </summary>
    TId Id { get; }
}