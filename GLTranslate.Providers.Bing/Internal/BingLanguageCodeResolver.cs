using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Domain.Linguistics.Languages;
using GLTranslate.Domain.Linguistics.Languages.Codes;
using System.Collections.Immutable;

namespace GLTranslate.Providers.Bing.Internal;

/// <summary>
/// Resolves between GLTranslate <see cref="LanguageId"/> values and the
/// language codes the Bing Translator endpoint expects.
/// </summary>
/// <remarks>
/// This resolver is provider-specific: it only exists so the Bing providers
/// can cross the boundary between the standard-independent domain model and
/// the ISO-639-1-based wire format of the endpoints.
/// </remarks>
internal static class BingLanguageCodeResolver
{
    private static readonly Lazy<ImmutableDictionary<string, LanguageId>> LanguagesByIso6391 = new(BuildIndex);

    // Bing does not speak plain ISO 639-1 everywhere: it names the script for
    // languages written in more than one, and uses the ISO 639-3 code for a few.
    private static readonly ImmutableDictionary<string, string> BingCodeByIso6391 =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["lg"] = "lug",
            ["no"] = "nb",
            ["ny"] = "nya",
            ["rn"] = "run",
            ["mn"] = "mn-Cyrl",
            ["sr"] = "sr-Cyrl",
            ["zh"] = "zh-Hans",
        }.ToImmutableDictionary();

    // On the way in the endpoint answers with the same codes it accepts, plus
    // the script variants it may return for a language the domain keeps as one.
    private static readonly ImmutableDictionary<string, string> Iso6391ByBingCode =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["lug"] = "lg",
            ["nb"] = "no",
            ["nn"] = "no",
            ["nya"] = "ny",
            ["run"] = "rn",
            ["mn-Cyrl"] = "mn",
            ["mn-Mong"] = "mn",
            ["sr-Cyrl"] = "sr",
            ["sr-Latn"] = "sr",
            ["zh-Hans"] = "zh",
            ["zh-Hant"] = "zh",
        }.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Converts a <see cref="LanguageId"/> into the code the endpoints expect.
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
    public static string ToBingCode(LanguageId languageId)
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
                BingProvider.Name,
                $"Language '{languageId.Value}' is not known to GLTranslate.",
                exception);
        }

        if (!language.Codes.TryGetValue(out Iso6391Code? code))
        {
            // The language is known to GLTranslate, but it has no ISO 639-1 code.
            throw new ProviderException(
                BingProvider.Name,
                $"Language '{languageId.Value}' has no ISO 639-1 code, which Bing requires.");
        }

        return BingCodeByIso6391.TryGetValue(code.Value, out string? bingCode)
            ? bingCode
            : code.Value;
    }

    /// <summary>
    /// Converts a language code returned by a Bing endpoint into the
    /// corresponding GLTranslate <see cref="LanguageId"/>.
    /// </summary>
    /// <param name="bingLanguageCode">
    /// The language code returned by the endpoint.
    /// </param>
    /// <returns>
    /// The identifier of the matching language.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="bingLanguageCode"/> is empty or consists
    /// only of white-space characters.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when <paramref name="bingLanguageCode"/> does not match any
    /// language known to GLTranslate.
    /// </exception>
    public static LanguageId FromBingCode(string bingLanguageCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bingLanguageCode);

        string code = bingLanguageCode.Trim();

        if (Iso6391ByBingCode.TryGetValue(code, out string? iso6391))
        {
            code = iso6391;
        }

        if (!LanguagesByIso6391.Value.TryGetValue(code.ToLowerInvariant(), out LanguageId? languageId))
        {
            // The endpoint returned a language code that is not known to GLTranslate.
            throw new ProviderException(
                BingProvider.Name,
                $"Bing returned an unknown language code '{bingLanguageCode}'.");
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
