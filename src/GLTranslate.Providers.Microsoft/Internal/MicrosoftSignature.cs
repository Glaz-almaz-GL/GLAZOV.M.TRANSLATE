using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace GLTranslate.Providers.Microsoft.Internal;

/// <summary>
/// Builds the <c>X-MT-Signature</c> header the Microsoft Translator endpoint
/// requires from a caller that has no subscription key.
/// </summary>
/// <remarks>
/// The endpoint accepts either an Azure subscription key or this signature,
/// which the Microsoft Translator mobile application uses. The signature is
/// an HMAC-SHA256 of the request path, the moment of the request and a fresh
/// identifier, so it is valid for one request only.
/// </remarks>
internal static class MicrosoftSignature
{
    private const string Prefix = "MSTranslatorAndroidApp";

    private const string TimestampFormat = @"ddd, dd MMM yyyy HH:mm:ssG\MT";

    // The key the Microsoft Translator mobile application signs its requests
    // with. It identifies the application, not a person, and carries no
    // subscription: it is what lets the endpoint answer a keyless caller.
    private static ReadOnlySpan<byte> PrivateKey =>
    [
        0xa2, 0x29, 0x3a, 0x3d, 0xd0, 0xdd, 0x32, 0x73,
        0x97, 0x7a, 0x64, 0xdb, 0xc2, 0xf3, 0x27, 0xf5,
        0xd7, 0xbf, 0x87, 0xd9, 0x45, 0x9d, 0xf0, 0x5a,
        0x09, 0x66, 0xc6, 0x30, 0xc6, 0x6a, 0xaa, 0x84,
        0x9a, 0x41, 0xaa, 0x94, 0x3a, 0xa8, 0xd5, 0x1a,
        0x6e, 0x4d, 0xaa, 0xc9, 0xa3, 0x70, 0x12, 0x35,
        0xc7, 0xeb, 0x12, 0xf6, 0xe8, 0x23, 0x07, 0x9e,
        0x47, 0x10, 0x95, 0x91, 0x88, 0x55, 0xd8, 0x17
    ];

    /// <summary>
    /// Builds a signature for the specified request.
    /// </summary>
    /// <param name="url">
    /// The request URL without its scheme, exactly as it is sent.
    /// </param>
    /// <returns>
    /// The value of the <c>X-MT-Signature</c> header.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="url"/> is empty or consists only of
    /// white-space characters.
    /// </exception>
    public static string Create(string url)
    {
        return Create(url, DateTimeOffset.UtcNow, Guid.NewGuid());
    }

    /// <summary>
    /// Builds a signature for the specified request at the specified moment.
    /// </summary>
    /// <param name="url">
    /// The request URL without its scheme, exactly as it is sent.
    /// </param>
    /// <param name="timestamp">
    /// The moment the request is made.
    /// </param>
    /// <param name="nonce">
    /// The identifier that makes the signature single-use.
    /// </param>
    /// <returns>
    /// The value of the <c>X-MT-Signature</c> header.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="url"/> is empty or consists only of
    /// white-space characters.
    /// </exception>
    public static string Create(string url, DateTimeOffset timestamp, Guid nonce)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        string formattedTimestamp = timestamp.UtcDateTime.ToString(TimestampFormat, CultureInfo.InvariantCulture);
        string formattedNonce = nonce.ToString("N");

        byte[] payload = Encoding.UTF8.GetBytes(
            $"{Prefix}{Uri.EscapeDataString(url)}{formattedTimestamp}{formattedNonce}".ToLowerInvariant());

        byte[] hash = HMACSHA256.HashData(PrivateKey, payload);

        return $"{Prefix}::{Convert.ToBase64String(hash)}::{formattedTimestamp}::{formattedNonce}";
    }
}
