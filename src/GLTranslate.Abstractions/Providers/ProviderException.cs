namespace GLTranslate.Abstractions.Providers;

/// <summary>
/// Represents an error raised by a provider while executing a capability
/// (for example a network failure, an unsupported language, or a
/// rejected request).
/// </summary>
/// <remarks>
/// <para>
/// Callers can catch this exception to handle provider failures uniformly,
/// independent of which provider or capability raised them. Providers
/// should throw this exception (or a more specific subclass of it) instead
/// of letting provider-specific exceptions (for example
/// <see cref="HttpRequestException"/>) escape their implementation.
/// </para>
/// <para>
/// This type is not sealed so providers can introduce more specific
/// subclasses (for example a rate-limit exception) without changing this
/// contract.
/// </para>
/// </remarks>
public class ProviderException : Exception
{
    /// <summary>
    /// Gets the display name of the provider that raised the exception.
    /// </summary>
    public string ProviderName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderException"/> class.
    /// </summary>
    /// <param name="providerName">
    /// The display name of the provider that raised the exception.
    /// </param>
    /// <param name="message">
    /// A message that describes the error.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="providerName"/> is empty or consists
    /// only of white-space characters.
    /// </exception>
    public ProviderException(string providerName, string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        ProviderName = providerName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderException"/> class.
    /// </summary>
    /// <param name="providerName">
    /// The display name of the provider that raised the exception.
    /// </param>
    /// <param name="message">
    /// A message that describes the error.
    /// </param>
    /// <param name="innerException">
    /// The exception that caused the current exception.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="providerName"/> is empty or consists
    /// only of white-space characters.
    /// </exception>
    public ProviderException(string providerName, string message, Exception innerException)
        : base(message, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        ProviderName = providerName;
    }
}
