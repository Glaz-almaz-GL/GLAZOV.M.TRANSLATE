using System.Diagnostics;

namespace GLTranslate.Abstractions.Common;

/// <summary>
/// Represents the base class for immutable value objects identified by
/// a single value of type <typeparamref name="TValue"/>.
/// </summary>
/// <typeparam name="TValue">
/// The type of the value represented by this object.
/// </typeparam>
/// <remarks>
/// <para>
/// Two value objects are considered equal when:
/// </para>
/// <list type="bullet">
/// <item>
/// They have the same runtime type.
/// </item>
/// <item>
/// Their <see cref="Value"/> properties are equal using
/// <see cref="EqualityComparer{T}.Default"/>.
/// </item>
/// </list>
/// <para>
/// Derived classes are immutable and thread-safe.
/// </para>
/// </remarks>
[DebuggerDisplay("{Value,nq}")]
public abstract class ValueObject<TValue>(TValue value) : IEquatable<ValueObject<TValue>>
{

    /// <summary>
    /// Gets the value represented by this object.
    /// </summary>
    public TValue Value { get; } = value;

    /// <inheritdoc />
    public bool Equals(ValueObject<TValue>? other)
    {
        return ReferenceEquals(this, other) ||
               (other is not null &&
                GetType() == other.GetType() &&
                EqualityComparer<TValue>.Default.Equals(Value, other.Value));
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is ValueObject<TValue> other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Value);
    }

    /// <summary>
    /// Determines whether two value objects are equal.
    /// </summary>
    public static bool operator ==(ValueObject<TValue>? left, ValueObject<TValue>? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Determines whether two value objects are not equal.
    /// </summary>
    public static bool operator !=(ValueObject<TValue>? left, ValueObject<TValue>? right)
    {
        return !Equals(left, right);
    }

    /// <summary>
    /// Returns the textual representation of the current value object.
    /// </summary>
    /// <returns>
    /// The string representation of the <see cref="Value"/> property.
    /// </returns>
    public override string ToString()
    {
        return Value?.ToString() ?? string.Empty;
    }
}