namespace GLAZOV.M.TRANSLATE.Providers.Common;

/// <summary>
/// Represents an engine that speaks on behalf of an account: the part every such
/// engine shares is that it is given the account's credentials and keeps them.
/// </summary>
/// <typeparam name="TCredentials">
/// The type of the credentials the provider's service asks for: an API key, or an
/// application identifier with a secret key.
/// </typeparam>
/// <remarks>
/// What the credentials are for — a header, a signature, a folder to bill — is
/// the derived engine's own business; this type only makes sure it has them.
/// </remarks>
public abstract class CredentialedEngine<TCredentials> : ProviderEngine
    where TCredentials : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialedEngine{TCredentials}"/>
    /// class with an <see cref="HttpClient"/> of its own.
    /// </summary>
    /// <param name="providerName">
    /// The name of the provider, which every failure reported here carries.
    /// </param>
    /// <param name="credentials">
    /// The credentials the engine speaks with.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when an argument is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="providerName"/> is empty or consists only of
    /// white-space characters.
    /// </exception>
    protected CredentialedEngine(string providerName, TCredentials credentials)
        : base(providerName)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        Credentials = credentials;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialedEngine{TCredentials}"/>
    /// class with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="providerName">
    /// The name of the provider, which every failure reported here carries.
    /// </param>
    /// <param name="credentials">
    /// The credentials the engine speaks with.
    /// </param>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when an argument is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="providerName"/> is empty or consists only of
    /// white-space characters.
    /// </exception>
    protected CredentialedEngine(string providerName, TCredentials credentials, HttpClient httpClient)
        : base(providerName, httpClient)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        Credentials = credentials;
    }

    /// <summary>
    /// Gets the credentials the engine speaks with.
    /// </summary>
    protected TCredentials Credentials { get; }
}
