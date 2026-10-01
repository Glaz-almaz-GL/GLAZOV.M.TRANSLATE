namespace GLAZOV.M.TRANSLATE.Providers.Papago;

/// <summary>
/// Settings of the Papago providers: where the Papago web service lives and in
/// which language it writes its own remarks.
/// </summary>
/// <remarks>
/// <para>
/// Papago has rewritten its web page and its calls before, so the address is a
/// setting and not a constant of the engine.
/// </para>
/// <para>
/// Instances are immutable and therefore thread-safe.
/// </para>
/// </remarks>
public sealed record PapagoOptions
{
    /// <summary>
    /// The default address of the Papago web service.
    /// </summary>
    public const string DefaultServiceUrl = "https://papago.naver.com";

    /// <summary>
    /// The default longest text, in characters, sent in one request.
    /// </summary>
    /// <remarks>
    /// Five thousand is the limit of the Papago web page itself.
    /// </remarks>
    public const int DefaultMaxTextLength = 5000;

    /// <summary>
    /// The default language of the service's own remarks.
    /// </summary>
    public const string DefaultInterfaceLocale = "en";

    /// <summary>
    /// The default most entries asked of the dictionary in one request.
    /// </summary>
    public const int DefaultDictionaryEntryLimit = 30;

    private readonly int _dictionaryEntryLimit = DefaultDictionaryEntryLimit;
    private readonly string _serviceUrl = DefaultServiceUrl;
    private readonly int _maxTextLength = DefaultMaxTextLength;
    private readonly string _interfaceLocale = DefaultInterfaceLocale;

    /// <summary>
    /// Gets the address of the Papago web service, without a trailing slash.
    /// Defaults to <see cref="DefaultServiceUrl"/>.
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

    /// <summary>
    /// Gets the most entries asked of the dictionary in one request. Defaults to
    /// <see cref="DefaultDictionaryEntryLimit"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when set to zero or a negative number.
    /// </exception>
    public int DictionaryEntryLimit
    {
        get => _dictionaryEntryLimit;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);

            _dictionaryEntryLimit = value;
        }
    }

    /// <summary>
    /// Gets the code of the language in which the service writes its own
    /// remarks, such as the explanation of a refusal. Defaults to
    /// <see cref="DefaultInterfaceLocale"/>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when set to an empty or white-space value.
    /// </exception>
    public string InterfaceLocale
    {
        get => _interfaceLocale;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);

            _interfaceLocale = value;
        }
    }
}
