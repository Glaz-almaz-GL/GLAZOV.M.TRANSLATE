using GLTranslate.Abstractions.Providers;

namespace GLTranslate.Providers.Common;

/// <summary>
/// Represents the part every provider engine of this library shares: the
/// <see cref="HttpClient"/> it talks through, who owns it, and the failures
/// it reports in the provider's name.
/// </summary>
/// <remarks>
/// <para>
/// An engine is the business logic of a provider and is never part of the
/// public API of one.
/// </para>
/// <para>
/// An engine either makes its own client and disposes it, or is given one and
/// leaves it alone. A derived engine with resources of its own overrides
/// <see cref="Dispose(bool)"/> and releases them there.
/// </para>
/// </remarks>
public abstract class ProviderEngine : IDisposable
{
    private readonly bool _ownsHttpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderEngine"/> class
    /// with an <see cref="HttpClient"/> of its own.
    /// </summary>
    /// <param name="providerName">
    /// The name of the provider, which every failure reported here carries.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="providerName"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="providerName"/> is empty or consists only
    /// of white-space characters.
    /// </exception>
    protected ProviderEngine(string providerName)
        : this(providerName, new HttpClient(), ownsHttpClient: true)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderEngine"/> class
    /// with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="providerName">
    /// The name of the provider, which every failure reported here carries.
    /// </param>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when an argument is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="providerName"/> is empty or consists only
    /// of white-space characters.
    /// </exception>
    protected ProviderEngine(string providerName, HttpClient httpClient)
        : this(providerName, httpClient ?? throw new ArgumentNullException(nameof(httpClient)), ownsHttpClient: false)
    {
    }

    private ProviderEngine(string providerName, HttpClient httpClient, bool ownsHttpClient)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        ProviderName = providerName;
        HttpClient = httpClient;
        _ownsHttpClient = ownsHttpClient;
    }

    /// <summary>
    /// Gets the name of the provider this engine speaks for.
    /// </summary>
    protected string ProviderName { get; }

    /// <summary>
    /// Gets the client the requests are sent with.
    /// </summary>
    protected HttpClient HttpClient { get; }

    /// <summary>
    /// Gets a value indicating whether the engine has been disposed.
    /// </summary>
    protected bool IsDisposed { get; private set; }

    /// <summary>
    /// Throws when the engine has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the engine has been disposed.
    /// </exception>
    protected void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
    }

    /// <summary>
    /// Builds the failure that reports a request which never got an answer.
    /// </summary>
    /// <param name="innerException">
    /// The failure of the request itself.
    /// </param>
    /// <returns>
    /// The failure to throw.
    /// </returns>
    protected ProviderException RequestFailed(Exception innerException)
    {
        return new ProviderException(ProviderName, $"The request to {ProviderName} failed.", innerException);
    }

    /// <summary>
    /// Builds the failure that reports an answer the engine cannot read.
    /// </summary>
    /// <param name="innerException">
    /// The failure of reading it.
    /// </param>
    /// <returns>
    /// The failure to throw.
    /// </returns>
    protected ProviderException UnreadableAnswer(Exception innerException)
    {
        return new ProviderException(ProviderName, $"{ProviderName} returned an unexpected response format.", innerException);
    }

    /// <summary>
    /// Releases the resources of the engine.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> when called from <see cref="Dispose()"/>.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing && _ownsHttpClient)
        {
            HttpClient.Dispose();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        Dispose(disposing: true);

        IsDisposed = true;

        GC.SuppressFinalize(this);
    }
}
