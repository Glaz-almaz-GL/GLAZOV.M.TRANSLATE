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
    /// <summary>
    /// Gets how the languages of GLTranslate are written in Bing's terms.
    /// </summary>
    internal static LanguageCodeResolver Instance { get; } = MicrosoftTranslatorLanguageCodes.CreateResolver(BingProvider.Name);

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
        return Instance.ToProviderCode(languageId);
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
        return Instance.FromProviderCode(bingLanguageCode);
    }
}
