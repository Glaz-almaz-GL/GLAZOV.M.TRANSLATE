namespace GLTranslate.Providers.GoogleCloud;

/// <summary>
/// Represents the API key every Google Cloud request is made with.
/// </summary>
/// <remarks>
/// <para>
/// The Google Cloud providers speak to the official APIs — Cloud Translation,
/// Cloud Text-to-Speech and Cloud Vision — which Google bills to the project
/// the key belongs to. The key is created in the Google Cloud console, and the
/// APIs the providers use have to be enabled for its project and allowed by
/// its restrictions.
/// </para>
/// <para>
/// The key is sent in a header, never in an address, and never appears in
/// <see cref="ToString"/>. Instances are immutable and thread-safe.
/// </para>
/// </remarks>
public sealed class GoogleCloudCredentials
{
    /// <summary>
    /// Gets the API key.
    /// </summary>
    internal string ApiKey { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleCloudCredentials"/> class.
    /// </summary>
    /// <param name="apiKey">
    /// The API key.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="apiKey"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="apiKey"/> is empty or consists only of
    /// white-space characters.
    /// </exception>
    public GoogleCloudCredentials(string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        ApiKey = apiKey.Trim();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return nameof(GoogleCloudCredentials);
    }
}
