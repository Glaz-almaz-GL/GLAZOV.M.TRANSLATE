using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Domain.Linguistics.Languages;
using GLTranslate.Domain.Linguistics.Languages.Codes;
using System.Collections.Immutable;

namespace GLTranslate.Providers.Microsoft.Internal;

/// <summary>
/// Resolves between GLTranslate <see cref="LanguageId"/> values and the
/// language codes the Microsoft Translator endpoint expects.
/// </summary>
/// <remarks>
/// This resolver is provider-specific: it only exists so
/// <see cref="MicrosoftTranslationProvider"/> can cross the boundary between
/// the standard-independent domain model and Microsoft's wire format.
/// </remarks>
internal static class MicrosoftLanguageCodeResolver
{
    private static readonly Lazy<ImmutableDictionary<string, LanguageId>> LanguagesByIso6391 = new(BuildIndex);

    // Microsoft does not speak plain ISO 639-1 everywhere. On the way out it
    // wants its own code for the languages listed here; the plain ISO 639-1
    // code is used for everything else.
    private static readonly ImmutableDictionary<string, string> MicrosoftCodeByIso6391 =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["lg"] = "lug",
            ["no"] = "nb",
            ["ny"] = "nya",
            ["rn"] = "run",
            // Microsoft names the script for languages written in more than one.
            ["mn"] = "mn-Cyrl",
            ["sr"] = "sr-Cyrl",
            ["zh"] = "zh-Hans",
        }.ToImmutableDictionary();

    // On the way in the endpoint answers with the same codes it accepts, so the
    // table above is read backwards, plus the script variants it may return for
    // a language the domain keeps as one.
    private static readonly ImmutableDictionary<string, string> Iso6391ByMicrosoftCode =
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
    /// Converts a <see cref="LanguageId"/> into the code Microsoft Translator
    /// expects.
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
    public static string ToMicrosoftCode(LanguageId languageId)
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
                MicrosoftProvider.Name,
                $"Language '{languageId.Value}' is not known to GLTranslate.",
                exception);
        }

        if (!language.Codes.TryGetValue(out Iso6391Code? code))
        {
            // The language is known to GLTranslate, but it has no ISO 639-1 code.
            throw new ProviderException(
                MicrosoftProvider.Name,
                $"Language '{languageId.Value}' has no ISO 639-1 code, which Microsoft Translator requires.");
        }

        return MicrosoftCodeByIso6391.TryGetValue(code.Value, out string? microsoftCode)
            ? microsoftCode
            : code.Value;
    }

    /// <summary>
    /// Converts a language code returned by Microsoft Translator into the
    /// corresponding GLTranslate <see cref="LanguageId"/>.
    /// </summary>
    /// <param name="microsoftLanguageCode">
    /// The language code returned by the endpoint.
    /// </param>
    /// <returns>
    /// The identifier of the matching language.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="microsoftLanguageCode"/> is empty or
    /// consists only of white-space characters.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when <paramref name="microsoftLanguageCode"/> does not match any
    /// language known to GLTranslate.
    /// </exception>
    public static LanguageId FromMicrosoftCode(string microsoftLanguageCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(microsoftLanguageCode);

        string code = microsoftLanguageCode.Trim();

        if (Iso6391ByMicrosoftCode.TryGetValue(code, out string? iso6391))
        {
            code = iso6391;
        }

        if (!LanguagesByIso6391.Value.TryGetValue(code.ToLowerInvariant(), out LanguageId? languageId))
        {
            // The endpoint returned a language code that is not known to GLTranslate.
            throw new ProviderException(
                MicrosoftProvider.Name,
                $"Microsoft Translator returned an unknown language code '{microsoftLanguageCode}'.");
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
