using GLTranslate.Abstractions.Common;
using System.Diagnostics;

namespace GLTranslate.Abstractions.Providers;

/// <summary>
/// Represents the unique identifier of a single provider capability request.
/// </summary>
/// <remarks>
/// <para>
/// Used to correlate a <see cref="ProviderResult"/> with the
/// <see cref="ProviderRequest"/> that produced it, independent of any
/// specific provider or capability.
/// </para>
/// <para>
/// Instances of this class are immutable and thread-safe.
/// </para>
/// </remarks>
[DebuggerDisplay("{Value,nq}")]
public sealed class RequestId : ValueObject<Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequestId"/> class with
    /// a newly generated value.
    /// </summary>
    public RequestId() : base(Guid.NewGuid())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestId"/> class with
    /// the specified value.
    /// </summary>
    /// <param name="value">
    /// The identifier value.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> is <see cref="Guid.Empty"/>.
    /// </exception>
    public RequestId(Guid value) : base(Validate(value))
    {
    }

    private static Guid Validate(Guid value)
    {
        if (value == Guid.Empty)
        {
            // Empty identifier
            throw new ArgumentException("Request identifier cannot be empty.", nameof(value));
        }

        return value;
    }
}
