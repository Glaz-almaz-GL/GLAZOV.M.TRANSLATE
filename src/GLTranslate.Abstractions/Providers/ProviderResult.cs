namespace GLTranslate.Abstractions.Providers;

/// <summary>
/// Represents the common shape of every provider capability result.
/// </summary>
/// <remarks>
/// Every result carries the <see cref="RequestId"/> of the
/// <see cref="ProviderRequest"/> it was produced from, so callers can
/// correlate results with requests regardless of the specific capability.
/// </remarks>
public abstract class ProviderResult
{
    /// <summary>
    /// Gets the identifier of the request this result was produced from.
    /// </summary>
    public RequestId RequestId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderResult"/> class.
    /// </summary>
    /// <param name="requestId">
    /// The identifier of the request this result was produced from.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="requestId"/> is <see langword="null"/>.
    /// </exception>
    protected ProviderResult(RequestId requestId)
    {
        ArgumentNullException.ThrowIfNull(requestId);

        RequestId = requestId;
    }
}
