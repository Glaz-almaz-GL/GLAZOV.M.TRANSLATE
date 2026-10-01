using GLTranslate.Abstractions.Interfaces;

namespace GLTranslate.Abstractions.Providers;

/// <summary>
/// Represents the common shape of every provider capability request.
/// </summary>
/// <remarks>
/// <para>
/// Every request carries a unique <see cref="RequestId"/> so its outcome
/// can be correlated with the corresponding <see cref="ProviderResult"/>,
/// independent of the specific capability.
/// </para>
/// <para>
/// Equality follows entity semantics: two requests are equal when their
/// identifiers are equal.
/// </para>
/// </remarks>
/// <param name="id">
/// The request identifier, or <see langword="null"/> to generate a new one.
/// </param>
public abstract class ProviderRequest(RequestId? id = null) :
    IIdentifiable<RequestId>,
    IEquatable<ProviderRequest>
{
    /// <inheritdoc/>
    public RequestId Id { get; } = id ?? new RequestId();

    /// <inheritdoc/>
    public bool Equals(ProviderRequest? other)
    {
        return ReferenceEquals(this, other) || (other is not null && Id.Equals(other.Id));
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is ProviderRequest other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    /// <summary>
    /// Determines whether two <see cref="ProviderRequest"/> instances
    /// represent the same request.
    /// </summary>
    public static bool operator ==(ProviderRequest? left, ProviderRequest? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Determines whether two <see cref="ProviderRequest"/> instances
    /// represent different requests.
    /// </summary>
    public static bool operator !=(ProviderRequest? left, ProviderRequest? right)
    {
        return !Equals(left, right);
    }
}
