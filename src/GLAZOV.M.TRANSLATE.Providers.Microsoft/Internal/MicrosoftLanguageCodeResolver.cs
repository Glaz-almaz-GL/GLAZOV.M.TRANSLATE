using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Providers.Common;

namespace GLAZOV.M.TRANSLATE.Providers.Microsoft.Internal;

/// <summary>
/// Resolves between GLAZOV.M.TRANSLATE <see cref="LanguageId"/> values and the
/// language codes the Microsoft Translator endpoint expects.
/// </summary>
/// <remarks>
/// This resolver is provider-specific: it holds the places where Microsoft
/// departs from plain ISO 639-1 and leaves the resolution itself to
/// <see cref="LanguageCodeResolver"/>.
/// </remarks>
internal static class MicrosoftLanguageCodeResolver
{
    /// <summary>
    /// Gets how the languages of GLAZOV.M.TRANSLATE are written in Microsoft's terms.
    /// </summary>
    internal static LanguageCodeResolver Instance { get; } = MicrosoftTranslatorLanguageCodes.CreateResolver(MicrosoftProvider.Name);

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
    /// Thrown when <paramref name="languageId"/> is not known to GLAZOV.M.TRANSLATE,
    /// or when the resolved language has no ISO 639-1 code.
    /// </exception>
    public static string ToMicrosoftCode(LanguageId languageId)
    {
        return Instance.ToProviderCode(languageId);
    }

    /// <summary>
    /// Converts a language code returned by Microsoft Translator into the
    /// corresponding GLAZOV.M.TRANSLATE <see cref="LanguageId"/>.
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
    /// language known to GLAZOV.M.TRANSLATE.
    /// </exception>
    public static LanguageId FromMicrosoftCode(string microsoftLanguageCode)
    {
        return Instance.FromProviderCode(microsoftLanguageCode);
    }
}
