using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Providers.Common;

namespace GLTranslate.Providers.Microsoft.Internal;

/// <summary>
/// Resolves between GLTranslate <see cref="LanguageId"/> values and the
/// language codes the Microsoft Translator endpoint expects.
/// </summary>
/// <remarks>
/// This resolver is provider-specific: it holds the places where Microsoft
/// departs from plain ISO 639-1 and leaves the resolution itself to
/// <see cref="LanguageCodeResolver"/>.
/// </remarks>
internal static class MicrosoftLanguageCodeResolver
{
    private static readonly LanguageCodeResolver Resolver = new(
        MicrosoftProvider.Name,
        [
            // Microsoft names the script for languages written in more than
            // one, and uses the ISO 639-3 code for a few.
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
        return Resolver.ToProviderCode(languageId);
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
        return Resolver.FromProviderCode(microsoftLanguageCode);
    }
}
