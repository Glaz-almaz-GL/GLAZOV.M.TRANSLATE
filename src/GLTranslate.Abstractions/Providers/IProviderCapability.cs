namespace GLTranslate.Abstractions.Providers;

/// <summary>
/// Represents a single operation a provider can perform.
/// </summary>
/// <remarks>
/// <para>
/// Providers implement one interface per capability they support
/// (for example <c>ITextTranslationProvider</c>) rather than declaring
/// support through a shared flags enumeration. Adding a new capability
/// therefore never requires modifying an existing type, consistent with
/// the Open/Closed Principle.
/// </para>
/// <para>
/// Consumers detect whether a provider supports a capability with a type
/// check (<c>provider is ISomeCapabilityProvider</c>).
/// </para>
/// </remarks>
/// <typeparam name="TRequest">
/// The immutable request type describing the operation's parameters.
/// </typeparam>
/// <typeparam name="TResult">
/// The immutable result type describing the operation's outcome.
/// </typeparam>
public interface IProviderCapability<in TRequest, TResult>
    where TRequest : ProviderRequest
    where TResult : ProviderResult
{
    /// <summary>
    /// Executes the capability for the specified request.
    /// </summary>
    /// <param name="request">
    /// The request describing the operation to perform.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task that completes with the result of the operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    Task<TResult> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
}
