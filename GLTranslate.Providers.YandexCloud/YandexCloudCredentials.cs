namespace GLTranslate.Providers.YandexCloud;

/// <summary>
/// Represents the identity every Yandex Cloud request is made with: an API key
/// and, when the key needs one, the folder that is billed.
/// </summary>
/// <remarks>
/// <para>
/// The Yandex Cloud providers speak to the official APIs — Translate, Vision
/// OCR and SpeechKit — which Yandex bills to a folder of a cloud. An API key of
/// a service account belongs to a folder already; a key of a user account
/// needs the folder to be named.
/// </para>
/// <para>
/// The key is sent in a header, never in an address, and never appears in
/// <see cref="ToString"/>. Instances are immutable and thread-safe.
/// </para>
/// </remarks>
public sealed class YandexCloudCredentials
{
    /// <summary>
    /// Gets the API key.
    /// </summary>
    internal string ApiKey { get; }

    /// <summary>
    /// Gets the identifier of the folder to bill, or <see langword="null"/>
    /// when the key belongs to one already.
    /// </summary>
    public string? FolderId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="YandexCloudCredentials"/> class.
    /// </summary>
    /// <param name="apiKey">
    /// The API key.
    /// </param>
    /// <param name="folderId">
    /// The identifier of the folder to bill, or <see langword="null"/> when the
    /// key belongs to one already.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="apiKey"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="apiKey"/> is empty or consists only of
    /// white-space characters, or when <paramref name="folderId"/> is given but
    /// empty or made of white-space characters.
    /// </exception>
    public YandexCloudCredentials(string apiKey, string? folderId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        if (folderId is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(folderId);
        }

        ApiKey = apiKey.Trim();
        FolderId = folderId?.Trim();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return FolderId is null
            ? nameof(YandexCloudCredentials)
            : $"{nameof(YandexCloudCredentials)} {{ FolderId = {FolderId} }}";
    }
}
