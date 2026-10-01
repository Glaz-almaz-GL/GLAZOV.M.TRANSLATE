using GLTranslate.Abstractions.Providers;
using GLTranslate.Providers.Common;

namespace GLTranslate.Providers.Baidu.Internal;

/// <summary>
/// Represents the part every engine of the Baidu Translate open platform shares:
/// the application it speaks for, and how it reads the code of an answer.
/// </summary>
internal abstract class BaiduEngineBase : CredentialedEngine<BaiduCredentials>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaiduEngineBase"/> class with
    /// an <see cref="HttpClient"/> of its own.
    /// </summary>
    /// <param name="credentials">
    /// The application the calls are made for.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="credentials"/> is <see langword="null"/>.
    /// </exception>
    protected BaiduEngineBase(BaiduCredentials credentials)
        : base(BaiduProvider.Name, credentials)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaiduEngineBase"/> class with
    /// the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="credentials">
    /// The application the calls are made for.
    /// </param>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when an argument is <see langword="null"/>.
    /// </exception>
    protected BaiduEngineBase(BaiduCredentials credentials, HttpClient httpClient)
        : base(BaiduProvider.Name, credentials, httpClient)
    {
    }

    /// <summary>
    /// Throws when the answer reports a failure.
    /// </summary>
    /// <param name="code">
    /// The code of the answer; zero means success.
    /// </param>
    /// <param name="message">
    /// The explanation of the answer.
    /// </param>
    /// <exception cref="ProviderException">
    /// Thrown when <paramref name="code"/> is not zero.
    /// </exception>
    protected void Ensure(int code, string? message)
    {
        if (code != 0)
        {
            throw RefusedWithCode(code, message);
        }
    }
}
