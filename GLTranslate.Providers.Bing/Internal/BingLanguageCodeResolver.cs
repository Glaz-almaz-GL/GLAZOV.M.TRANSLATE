using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Providers.Common;

namespace GLTranslate.Providers.Bing.Internal;

/// <summary>
/// Resolves between GLTranslate <see cref="LanguageId"/> values and the
/// language codes the Bing Translator endpoint expects.
/// </summary>
/// <remarks>
/// This resolver is provider-specific: it holds the places where Bing departs
/// from plain ISO 639-1 and leaves the resolution itself to
/// <see cref="LanguageCodeResolver"/>.
/// </remarks>
internal static class BingLanguageCodeResolver
{
    private static readonly LanguageCodeResolver Resolver = new(
        BingProvider.Name,
        [
            // Bing names the script for languages written in more than one, and
            // uses the ISO 639-3 code for a few.
            new("lg", "lug"),
            new("no", "nb"),
            new("ny", "nya"),
            new("rn", "run"),
            new("mn", "mn-Cyrl"),
            new("sr", "sr-Cyrl"),
            new("zh", "zh-Hans"),
        ],
        [
            new("lug", "lg"),
            new("nb", "no"),
            new("nn", "no"),
            new("nya", "ny"),
            new("run", "rn"),
            new("mn-Cyrl", "mn"),
            new("mn-Mong", "mn"),
            new("sr-Cyrl", "sr"),
            new("sr-Latn", "sr"),
            new("zh-Hans", "zh"),
            new("zh-Hant", "zh"),
        ]);

    /// <summary>
    /// Converts a <see cref="LanguageId"/> into the code Bing expects.
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
        return Resolver.ToProviderCode(languageId);
    }

    /// <summary>
    /// Converts a language code returned by Bing into the corresponding
    /// GLTranslate <see cref="LanguageId"/>.
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
        return Resolver.FromProviderCode(bingLanguageCode);
    }
}
