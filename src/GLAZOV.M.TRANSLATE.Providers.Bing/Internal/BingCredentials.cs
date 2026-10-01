namespace GLAZOV.M.TRANSLATE.Providers.Bing.Internal;

/// <summary>
/// Represents the credentials the Bing translator page hands to the script
/// running on it, which the endpoint expects every request to carry.
/// </summary>
/// <param name="Token">
/// The token the page was given.
/// </param>
/// <param name="Key">
/// The moment the page was built, in milliseconds since the Unix epoch, which
/// the endpoint expects alongside the token.
/// </param>
/// <param name="ImpressionGuid">
/// The identifier of this session with the page, made up by the caller.
/// </param>
/// <param name="ExpiresAt">
/// The moment the pair stops being accepted.
/// </param>
internal readonly record struct BingCredentials(
    string Token,
    long Key,
    Guid ImpressionGuid,
    DateTimeOffset ExpiresAt)
{
    /// <summary>
    /// Gets a value indicating whether the credentials are no longer accepted.
    /// </summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
}
