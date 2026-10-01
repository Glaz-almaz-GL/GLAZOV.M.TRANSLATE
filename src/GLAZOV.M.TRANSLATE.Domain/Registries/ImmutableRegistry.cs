using GLAZOV.M.TRANSLATE.Abstractions.Common;
using GLAZOV.M.TRANSLATE.Abstractions.Interfaces;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace GLAZOV.M.TRANSLATE.Domain.Registries;

/// <summary>
/// Represents a base implementation of an immutable registry for domain entities.
/// </summary>
/// <typeparam name="TEntity">
/// The type of entity stored in the registry.
/// </typeparam>
/// <typeparam name="TId">
/// The type of the entity identifier.
/// </typeparam>
/// <remarks>
/// Provides thread-safe, immutable lookup operations for entities identified
/// by a strongly typed identifier.
/// </remarks>
public abstract partial class ImmutableRegistry<TEntity, TId> :
    IRegistry<TEntity, TId>
    where TId : notnull
    where TEntity : class, IIdentifiable<TId>
{
    private readonly ImmutableDictionary<TId, TEntity> _entities;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImmutableRegistry{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="entities">
    /// The entities contained in the registry.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entities"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when duplicate entity identifiers are found.
    /// </exception>
    protected ImmutableRegistry(IEnumerable<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        TEntity[] values = [.. entities];

        foreach (TEntity entity in values)
        {
            ArgumentNullException.ThrowIfNull(entity);
        }

        _entities = values.ToImmutableDictionary(
            entity => entity.Id,
            entity => entity);

        All = new EntitySet<TEntity, TId>(values);
    }

    /// <inheritdoc/>
    public EntitySet<TEntity, TId> All { get; }

    /// <inheritdoc/>
    public TEntity Get(TId id)
    {
        ArgumentNullException.ThrowIfNull(id);

        if (!_entities.TryGetValue(id, out TEntity? entity))
        {
            // Entity not found
            throw new KeyNotFoundException($"Entity with identifier '{id}' was not found.");
        }

        return entity;
    }

    /// <inheritdoc/>
    public bool TryGet(TId id, [NotNullWhen(true)] out TEntity? entity)
    {
        ArgumentNullException.ThrowIfNull(id);

        return _entities.TryGetValue(id, out entity);
    }

    /// <inheritdoc/>
    public bool Contains(TId id)
    {
        ArgumentNullException.ThrowIfNull(id);

        return _entities.ContainsKey(id);
    }
}