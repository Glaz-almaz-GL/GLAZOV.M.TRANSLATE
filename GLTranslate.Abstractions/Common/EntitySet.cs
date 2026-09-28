using GLTranslate.Abstractions.Interfaces;
using System.Collections;
using System.Collections.Immutable;

namespace GLTranslate.Abstractions.Common;

/// <summary>
/// Represents an immutable collection of unique domain entities.
/// </summary>
/// <remarks>
/// The collection does not allow null elements or duplicate entities.
///
/// Entity uniqueness is determined according to entity equality rules.
///
/// The collection is immutable and thread-safe after construction.
/// </remarks>
/// <typeparam name="TEntity">
/// The entity type contained in the collection.
/// </typeparam>
public sealed class EntitySet<TEntity, TId> :
    IReadOnlyList<TEntity>
    where TId : notnull
    where TEntity :
    class,
    IIdentifiable<TId>
{
    private readonly ImmutableArray<TEntity> _values;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntitySet{TEntity}"/> class.
    /// </summary>
    /// <param name="entities">
    /// The entities to include in the collection.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entities"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the collection contains null elements
    /// or duplicate entities.
    /// </exception>
    public EntitySet(IEnumerable<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        TEntity[] values = [.. entities];

        HashSet<TEntity> uniqueEntities = [];

        foreach (TEntity entity in values)
        {
            ArgumentNullException.ThrowIfNull(entity);

            if (!uniqueEntities.Add(entity))
            {
                throw new ArgumentException($"Duplicate entities are not allowed. Entity '{entity}' is duplicated.", nameof(entities));
            }
        }

        _values = [.. values];
    }

    /// <summary>
    /// Gets the entity at the specified index.
    /// </summary>
    /// <param name="index">
    /// The zero-based index of the entity to retrieve.
    /// </param>
    /// <returns>
    /// The entity stored at the specified index.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is outside the valid range.
    /// </exception>
    public TEntity this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _values.Length);

            return _values[index];
        }
    }

    /// <summary>
    /// Gets the number of entities in the collection.
    /// </summary>
    public int Count => _values.Length;

    /// <summary>
    /// Determines whether the collection contains the specified entity.
    /// </summary>
    /// <param name="entity">
    /// The entity to locate.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the entity exists in the collection;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entity"/> is <see langword="null"/>.
    /// </exception>
    public bool Contains(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return _values.Contains(entity);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>
    /// An allocation-free enumerator over the immutable collection.
    /// </returns>
    public ImmutableArray<TEntity>.Enumerator GetEnumerator()
    {
        return _values.GetEnumerator();
    }

    IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator()
    {
        return ((IEnumerable<TEntity>)_values).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_values).GetEnumerator();
    }
}