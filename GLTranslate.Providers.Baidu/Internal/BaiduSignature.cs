using System.Security.Cryptography;
using System.Text;

namespace GLTranslate.Providers.Baidu.Internal;

/// <summary>
/// Signs the requests of the Baidu Translate open platform.
/// </summary>
/// <remarks>
/// The platform accepts a request only with a signature: the lowercase
/// hexadecimal MD5 of the identifying values of the request glued to the secret
/// key of the application. The order of the values differs between the text
/// and the image endpoints, which is why each has its own method.
/// </remarks>
internal static class BaiduSignature
{
    /// <summary>
    /// Signs a text request.
    /// </summary>
    /// <param name="appId">
    /// The identifier of the application.
    /// </param>
    /// <param name="text">
    /// The text to translate, exactly as it is sent, before it is URL-encoded.
    /// </param>
    /// <param name="salt">
    /// The random string that goes with the request.
    /// </param>
    /// <param name="secretKey">
    /// The secret key of the application.
    /// </param>
    /// <returns>
    /// The signature: 32 lowercase hexadecimal characters.
    /// </returns>
    public static string ForText(string appId, string text, string salt, string secretKey)
    {
        return Md5(appId + text + salt + secretKey);
    }

    /// <summary>
    /// Signs an image request.
    /// </summary>
    /// <param name="appId">
    /// The identifier of the application.
    /// </param>
    /// <param name="image">
    /// The bytes of the image, exactly as they are sent.
    /// </param>
    /// <param name="salt">
    /// The random string that goes with the request.
    /// </param>
    /// <param name="deviceId">
    /// The device identifier that goes with the request.
    /// </param>
    /// <param name="mac">
    /// The address field that goes with the request.
    /// </param>
    /// <param name="secretKey">
    /// The secret key of the application.
    /// </param>
    /// <returns>
    /// The signature: 32 lowercase hexadecimal characters.
    /// </returns>
    public static string ForImage(
        string appId,
        ReadOnlySpan<byte> image,
        string salt,
        string deviceId,
        string mac,
        string secretKey)
    {
        return Md5(appId + Convert.ToHexStringLower(MD5.HashData(image)) + salt + deviceId + mac + secretKey);
    }

    /// <summary>
    /// Signs a request of the endpoints that carry their identity in headers,
    /// which are the document and the speech ones.
    /// </summary>
    /// <param name="appId">
    /// The identifier of the application.
    /// </param>
    /// <param name="timestamp">
    /// The Unix time in seconds that goes with the request, as the header
    /// carries it.
    /// </param>
    /// <param name="payload">
    /// What is signed: the body of a document request exactly as it is sent, or
    /// the base64 audio of a speech one.
    /// </param>
    /// <param name="secretKey">
    /// The secret key of the application.
    /// </param>
    /// <returns>
    /// The signature: the base64 of the HMAC-SHA256 of the application
    /// identifier, the timestamp and the payload glued together, keyed by the
    /// secret key.
    /// </returns>
    public static string ForHeaders(string appId, string timestamp, string payload, string secretKey)
    {
        byte[] hash = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(secretKey),
            Encoding.UTF8.GetBytes(appId + timestamp + payload));

        return Convert.ToBase64String(hash);
    }

    private static string Md5(string value)
    {
        return Convert.ToHexStringLower(MD5.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
