using GLAZOV.M.TRANSLATE.Abstractions.Interfaces;
using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace GLAZOV.M.TRANSLATE.Abstractions.Common;

/// <summary>
/// Represents an immutable collection of strongly typed codes
/// belonging to a single domain concept.
/// </summary>
/// <remarks>
/// Each code type may appear only once in the collection.
/// Duplicate code types are prohibited.
///
/// The collection is immutable and thread-safe after construction.
/// </remarks>
/// <typeparam name="TCode">
/// The base type of codes contained in the collection.
/// </typeparam>
public sealed class CodeSet<TCode> : IReadOnlyList<TCode>
    where TCode : class, ICode
{
    private readonly ImmutableArray<TCode> _values;

    private readonly ImmutableDictionary<Type, TCode> _codes;

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeSet{TCode}"/> class.
    /// </summary>
    /// <param name="codes">
    /// The collection of codes to include.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="codes"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the collection contains null elements
    /// or multiple codes of the same runtime type.
    /// </exception>
    public CodeSet(IEnumerable<TCode> codes)
    {
        ArgumentNullException.ThrowIfNull(codes);

        TCode[] values = [.. codes];

        HashSet<Type> types = [];

        foreach (TCode code in values)
        {
            ArgumentNullException.ThrowIfNull(code);

            if (!types.Add(code.GetType()))
            {
                throw new ArgumentException(
                    $"Duplicate code types are not allowed. Type '{code.GetType().Name}' is duplicated.",
                    nameof(codes));
            }
        }

        _values = [.. values];

        _codes = _values.ToImmutableDictionary(
            x => x.GetType(),
            x => x);
    }

    /// <summary>
    /// Gets a code of the specified type.
    /// </summary>
    /// <typeparam name="T">
    /// The requested code type.
    /// </typeparam>
    /// <returns>
    /// The code instance of the requested type.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the requested code type is not available.
    /// </exception>
    public T Get<T>()
        where T : class, TCode
    {
        if (!_codes.TryGetValue(typeof(T), out TCode? value))
        {
            // Code unavailable
            throw new InvalidOperationException($"Code '{typeof(T).Name}' is unavailable.");
        }

        return (T)value;
    }

    /// <summary>
    /// Gets a code by its runtime type.
    /// </summary>
    /// <param name="type">
    /// The runtime type of the requested code.
    /// </param>
    /// <returns>
    /// The code instance of the specified type.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="type"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the specified type is not compatible with
    /// <typeparamref name="TCode"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the requested code type is not available.
    /// </exception>
    public TCode Get(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (!typeof(TCode).IsAssignableFrom(type))
        {
            // Not assignable type
            throw new ArgumentException($"'{type.Name}' is not assignable to '{typeof(TCode).Name}'.", nameof(type));
        }

        if (!_codes.TryGetValue(type, out TCode? value))
        {
            // Unavailable code
            throw new InvalidOperationException($"Code '{type.Name}' is unavailable.");
        }

        return value;
    }

    /// <summary>
    /// Attempts to get a code of the specified type.
    /// </summary>
    /// <typeparam name="T">
    /// The requested code type.
    /// </typeparam>
    /// <param name="value">
    /// When this method returns <see langword="true"/>,
    /// contains the requested code.
    /// Otherwise, contains <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the code exists;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGetValue<T>([NotNullWhen(true)] out T? value)
        where T : class, TCode
    {
        if (_codes.TryGetValue(typeof(T), out TCode? code))
        {
            value = (T)code;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Attempts to get a code by its runtime type.
    /// </summary>
    /// <param name="type">
    /// The runtime type of the requested code.
    /// </param>
    /// <param name="value">
    /// When this method returns <see langword="true"/>,
    /// contains the requested code.
    /// Otherwise, contains <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the code exists;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="type"/> is <see langword="null"/>.
    /// </exception>
    public bool TryGetValue(Type type, [NotNullWhen(true)] out TCode? value)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (!typeof(TCode).IsAssignableFrom(type))
        {
            // Not assignable type
            value = null;
            return false;
        }

        return _codes.TryGetValue(type, out value);
    }

    /// <summary>
    /// Determines whether a code of the specified type exists.
    /// </summary>
    /// <typeparam name="T">
    /// The code type to search for.
    /// </typeparam>
    /// <returns>
    /// <see langword="true"/> when the code exists;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool Contains<T>()
        where T : class, TCode
    {
        return _codes.ContainsKey(typeof(T));
    }

    /// <summary>
    /// Determines whether a code of the specified runtime type exists.
    /// </summary>
    /// <param name="type">
    /// The runtime type of the code.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the code exists;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="type"/> is <see langword="null"/>.
    /// </exception>
    public bool Contains(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return _codes.ContainsKey(type);
    }

    /// <summary>
    /// Gets the code at the specified index.
    /// </summary>
    /// <param name="index">
    /// The zero-based index of the code to retrieve.
    /// </param>
    /// <returns>
    /// The code stored at the specified index.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is outside the valid range.
    /// </exception>
    public TCode this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _values.Length);

            return _values[index];
        }
    }

    /// <summary>
    /// Gets the number of codes in the collection.
    /// </summary>
    public int Count => _values.Length;

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>
    /// An allocation-free enumerator over the immutable collection.
    /// </returns>
    public ImmutableArray<TCode>.Enumerator GetEnumerator()
    {
        return _values.GetEnumerator();
    }

    IEnumerator<TCode> IEnumerable<TCode>.GetEnumerator()
    {
        return ((IEnumerable<TCode>)_values).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_values).GetEnumerator();
    }
}