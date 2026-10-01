namespace GLAZOV.M.TRANSLATE.Providers.Google;

/// <summary>
/// Settings of <see cref="GoogleBatchExecuteTranslationProvider"/>: where the
/// Google Translate web service lives and how its internal call is named.
/// </summary>
/// <remarks>
/// <para>
/// Every value here is something Google may change without notice, which is
/// why none of them is written into the engine: when the service moves or
/// renames its call, the caller changes the setting instead of waiting for a
/// new version of the library.
/// </para>
/// <para>
/// Instances are immutable and therefore thread-safe.
/// </para>
/// </remarks>
public sealed record GoogleBatchExecuteOptions
{
    /// <summary>
    /// The default address of the Google Translate web service.
    /// </summary>
    public const string DefaultServiceUrl = "https://translate.google.com";

    /// <summary>
    /// The default name of the web service's translation call.
    /// </summary>
    public const string DefaultRpcId = "MkEWBc";

    /// <summary>
    /// The default longest text, in characters, sent in one request.
    /// </summary>
    /// <remarks>
    /// Google answers a request of about nine thousand characters with a
    /// refusal and one of about four thousand with a translation; five
    /// thousand is the limit its own web page enforces.
    /// </remarks>
    public const int DefaultMaxTextLength = 5000;

    /// <summary>
    /// The default name of the web service's speech call.
    /// </summary>
    public const string DefaultSpeechRpcId = "jQ1olc";

    /// <summary>
    /// The default name of the web service's suggestion call.
    /// </summary>
    public const string DefaultSuggestionsRpcId = "AVdN8";

    /// <summary>
    /// The default longest text, in characters, spoken in one request.
    /// </summary>
    /// <remarks>
    /// The service refuses a speech request of about two hundred and fifty
    /// characters and accepts one of two hundred; longer text is cut into pieces
    /// at spaces and the pieces are spoken one by one.
    /// </remarks>
    public const int DefaultMaxSpeechChunkLength = 200;

    private readonly string _serviceUrl = DefaultServiceUrl;
    private readonly string _rpcId = DefaultRpcId;
    private readonly string _speechRpcId = DefaultSpeechRpcId;
    private readonly string _suggestionsRpcId = DefaultSuggestionsRpcId;
    private readonly int _maxTextLength = DefaultMaxTextLength;
    private readonly int _maxSpeechChunkLength = DefaultMaxSpeechChunkLength;

    /// <summary>
    /// Gets the address of the Google Translate web service, without a
    /// trailing slash. Defaults to <see cref="DefaultServiceUrl"/>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when set to a value that is not an absolute <c>http</c> or
    /// <c>https</c> address.
    /// </exception>
    public string ServiceUrl
    {
        get => _serviceUrl;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);

            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
                || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            {
                throw new ArgumentException("The service address must be an absolute http or https address.", nameof(value));
            }

            _serviceUrl = value.TrimEnd('/');
        }
    }

    /// <summary>
    /// Gets the name of the web service's translation call. Defaults to
    /// <see cref="DefaultRpcId"/>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when set to an empty or white-space value.
    /// </exception>
    public string RpcId
    {
        get => _rpcId;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);

            _rpcId = value;
        }
    }

    /// <summary>
    /// Gets the name of the web service's speech call. Defaults to
    /// <see cref="DefaultSpeechRpcId"/>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when set to an empty or white-space value.
    /// </exception>
    public string SpeechRpcId
    {
        get => _speechRpcId;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);

            _speechRpcId = value;
        }
    }

    /// <summary>
    /// Gets the name of the web service's suggestion call. Defaults to
    /// <see cref="DefaultSuggestionsRpcId"/>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when set to an empty or white-space value.
    /// </exception>
    public string SuggestionsRpcId
    {
        get => _suggestionsRpcId;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);

            _suggestionsRpcId = value;
        }
    }

    /// <summary>
    /// Gets the longest text, in characters, spoken in one request. Defaults to
    /// <see cref="DefaultMaxSpeechChunkLength"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when set to zero or a negative number.
    /// </exception>
    public int MaxSpeechChunkLength
    {
        get => _maxSpeechChunkLength;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);

            _maxSpeechChunkLength = value;
        }
    }

    /// <summary>
    /// Gets the build label of the web page sent with every request, or an
    /// empty string to send none. Defaults to an empty string.
    /// </summary>
    /// <remarks>
    /// The service accepts a request without a label, so the library sends
    /// none unless asked to.
    /// </remarks>
    public string BuildLabel { get; init; } = string.Empty;

    /// <summary>
    /// Gets the longest text, in characters, sent in one request. Defaults to
    /// <see cref="DefaultMaxTextLength"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when set to zero or a negative number.
    /// </exception>
    public int MaxTextLength
    {
        get => _maxTextLength;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);

            _maxTextLength = value;
        }
    }
}
