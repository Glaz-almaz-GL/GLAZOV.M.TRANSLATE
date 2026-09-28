using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Domain.Linguistics.Languages;
using GLTranslate.Domain.Linguistics.Languages.Codes;
using System.Collections.Immutable;

namespace GLTranslate.Providers.Common;

/// <summary>
/// Resolves between GLTranslate <see cref="LanguageId"/> values and the
/// ISO 639-1 codes a provider speaks.
/// </summary>
/// <remarks>
/// <para>
/// Every provider of this library speaks ISO 639-1, and every one of them
/// departs from it for a handful of languages: it names the script for a
/// language written in more than one, keeps a code the standard withdrew, or
/// splits one language into several. The departures are the only difference
/// between one provider's resolution and another's, so they are given as two
/// tables and the rest is done here.
/// </para>
/// <para>
/// Instances of <see cref="LanguageCodeResolver"/> are immutable and
/// thread-safe.
/// </para>
/// </remarks>
public sealed class LanguageCodeResolver
{
    private static readonly Lazy<ImmutableDictionary<string, LanguageId>> LanguagesByIso6391 = new(BuildIndex);

    private readonly string _providerName;
    private readonly ImmutableDictionary<string, string> _providerCodeByIso6391;
    private readonly ImmutableDictionary<string, string> _iso6391ByProviderCode;

    /// <summary>
    /// Initializes a new instance of the <see cref="LanguageCodeResolver"/> class.
    /// </summary>
    /// <param name="providerName">
    /// The name of the provider, which every <see cref="ProviderException"/>
    /// thrown here carries.
    /// </param>
    /// <param name="providerCodeByIso6391">
    /// The codes the provider wants instead of the plain ISO 639-1 one, keyed
    /// by that code. A language absent from it is sent as its ISO 639-1 code.
    /// </param>
    /// <param name="iso6391ByProviderCode">
    /// The ISO 639-1 codes of the codes the provider answers with that are not
    /// ISO 639-1 themselves, keyed by what it answers. Case does not matter.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when one of the arguments is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="providerName"/> is empty or consists only of
    /// white-space characters.
    /// </exception>
    public LanguageCodeResolver(
        string providerName,
        IEnumerable<KeyValuePair<string, string>> providerCodeByIso6391,
        IEnumerable<KeyValuePair<string, string>> iso6391ByProviderCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        ArgumentNullException.ThrowIfNull(providerCodeByIso6391);
        ArgumentNullException.ThrowIfNull(iso6391ByProviderCode);

        _providerName = providerName;
        _providerCodeByIso6391 = providerCodeByIso6391.ToImmutableDictionary(StringComparer.Ordinal);
        _iso6391ByProviderCode = iso6391ByProviderCode.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LanguageCodeResolver"/>
    /// class for a provider that speaks plain ISO 639-1 throughout.
    /// </summary>
    /// <param name="providerName">
    /// The name of the provider.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="providerName"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="providerName"/> is empty or consists only of
    /// white-space characters.
    /// </exception>
    public LanguageCodeResolver(string providerName)
        : this(providerName, [], [])
    {
    }

    /// <summary>
    /// Converts a <see cref="LanguageId"/> into the code the provider expects.
    /// </summary>
    /// <param name="languageId">
    /// The language identifier.
    /// </param>
    /// <returns>
    /// The language code to send.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="languageId"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when <paramref name="languageId"/> is not known to GLTranslate,
    /// or when the resolved language has no ISO 639-1 code.
    /// </exception>
    public string ToProviderCode(LanguageId languageId)
    {
        ArgumentNullException.ThrowIfNull(languageId);

        Language language;

        try
        {
            language = LanguageRegistry.Default.Get(languageId);
        }
        catch (KeyNotFoundException exception)
        {
            throw new ProviderException(
                _providerName,
                $"Language '{languageId.Value}' is not known to GLTranslate.",
                exception);
        }

        if (!language.Codes.TryGetValue(out Iso6391Code? code))
        {
            // The language is known to GLTranslate, but it has no ISO 639-1 code.
            throw new ProviderException(
                _providerName,
                $"Language '{languageId.Value}' has no ISO 639-1 code, which {_providerName} requires.");
        }

        return _providerCodeByIso6391.TryGetValue(code.Value, out string? providerCode)
            ? providerCode
            : code.Value;
    }

    /// <summary>
    /// Converts a language code the provider answered with into the
    /// corresponding GLTranslate <see cref="LanguageId"/>.
    /// </summary>
    /// <param name="providerLanguageCode">
    /// The language code the provider answered with.
    /// </param>
    /// <returns>
    /// The identifier of the matching language.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="providerLanguageCode"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="providerLanguageCode"/> is empty or consists
    /// only of white-space characters.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when <paramref name="providerLanguageCode"/> does not match any
    /// language known to GLTranslate.
    /// </exception>
    public LanguageId FromProviderCode(string providerLanguageCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerLanguageCode);

        string code = providerLanguageCode.Trim();

        if (_iso6391ByProviderCode.TryGetValue(code, out string? iso6391))
        {
            code = iso6391;
        }

        if (!LanguagesByIso6391.Value.TryGetValue(code.ToLowerInvariant(), out LanguageId? languageId))
        {
            // The provider answered with a language code that is not known to GLTranslate.
            throw new ProviderException(
                _providerName,
                $"{_providerName} returned an unknown language code '{providerLanguageCode}'.");
        }

        return languageId;
    }

    private static ImmutableDictionary<string, LanguageId> BuildIndex()
    {
        ImmutableDictionary<string, LanguageId>.Builder index =
            ImmutableDictionary.CreateBuilder<string, LanguageId>(StringComparer.Ordinal);

        foreach (Language language in LanguageRegistry.Default.All)
        {
            if (language.Codes.TryGetValue(out Iso6391Code? code))
            {
                index[code.Value] = language.Id;
            }
        }

        return index.ToImmutable();
    }
}
