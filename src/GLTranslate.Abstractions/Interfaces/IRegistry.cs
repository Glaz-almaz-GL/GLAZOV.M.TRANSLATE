using GLTranslate.Abstractions.Common;
using System.Diagnostics.CodeAnalysis;

namespace GLTranslate.Abstractions.Interfaces;

/// <summary>
/// Represents an immutable lookup service for domain entities.
/// </summary>
/// <typeparam name="TEntity">
/// The entity type stored in the registry.
/// </typeparam>
/// <typeparam name="TId">
/// The identifier type of the entity.
/// </typeparam>
public interface IRegistry<TEntity, TId>
    where TId : notnull
    where TEntity : class, IIdentifiable<TId>
{
    /// <summary>
    /// Gets all entities contained in the registry.
    /// </summary>
    EntitySet<TEntity, TId> All { get; }


    /// <summary>
    /// Gets an entity by its identifier.
    /// </summary>
    /// <param name="id">
    /// The entity identifier.
    /// </param>
    /// <returns>
    /// The entity associated with the identifier.
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the entity does not exist.
    /// </exception>
    TEntity Get(TId id);


    /// <summary>
    /// Attempts to get an entity by its identifier.
    /// </summary>
    /// <param name="id">
    /// The entity identifier.
    /// </param>
    /// <param name="entity">
    /// The entity when found; otherwise <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the entity exists;
    /// otherwise <see langword="false"/>.
    /// </returns>
    bool TryGet(TId id, [NotNullWhen(true)] out TEntity? entity);


    /// <summary>
    /// Determines whether an entity exists in the registry.
    /// </summary>
    /// <param name="id">
    /// The entity identifier.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the entity exists;
    /// otherwise <see langword="false"/>.
    /// </returns>
    bool Contains(TId id);
}