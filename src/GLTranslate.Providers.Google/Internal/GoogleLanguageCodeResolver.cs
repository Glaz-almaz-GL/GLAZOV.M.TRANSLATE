using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Providers.Common;

namespace GLTranslate.Providers.Google.Internal;

/// <summary>
/// Resolves between GLTranslate <see cref="LanguageId"/> values and the
/// language codes the Google Translate web endpoint expects.
/// </summary>
/// <remarks>
/// This resolver is provider-specific: it holds the places where Google
/// departs from plain ISO 639-1, and the code that asks it to detect the
/// source language itself, and leaves the resolution to
/// <see cref="LanguageCodeResolver"/>.
/// </remarks>
internal static class GoogleLanguageCodeResolver
{
    internal const string AutoDetectCode = "auto";

    /// <summary>
    /// Gets how the languages of GLTranslate are written in the provider's terms.
    /// </summary>
    internal static LanguageCodeResolver Instance { get; } = new(
        GoogleProvider.Name,
        [
            // Google has no plain "zh": it asks which Chinese. The domain model
            // writes Chinese in the simplified script, so that is what is sent.
            new("zh", "zh-CN"),
        ],
        [
            // Google answers with codes withdrawn from the standard in 1989,
            // and with Chinese split by script.
            new("iw", "he"),
            new("jw", "jv"),
            new("in", "id"),
            new("ji", "yi"),
            new("mo", "ro"),
            new("nb", "no"),
            new("nn", "no"),
            new("zh-CN", "zh"),
            new("zh-TW", "zh"),
            new("zh-Hans", "zh"),
            new("zh-Hant", "zh"),
        ]);

    /// <summary>
    /// Converts a <see cref="LanguageId"/> into the code Google Translate
    /// expects.
    /// </summary>
    /// <param name="languageId">
    /// The language identifier, or <see langword="null"/> to request
    /// automatic source language detection.
    /// </param>
    /// <returns>
    /// The language code, or <c>"auto"</c> when <paramref name="languageId"/>
    /// is <see langword="null"/>.
    /// </returns>
    /// <exception cref="ProviderException">
    /// Thrown when <paramref name="languageId"/> is not known to GLTranslate,
    /// or when the resolved language has no ISO 639-1 code.
    /// </exception>
    public static string ToGoogleCode(LanguageId? languageId)
    {
        return languageId is null ? AutoDetectCode : Instance.ToProviderCode(languageId);
    }

    /// <summary>
    /// Converts a language code returned by Google Translate into the
    /// corresponding GLTranslate <see cref="LanguageId"/>.
    /// </summary>
    /// <param name="googleLanguageCode">
    /// The language code returned by the Google Translate web endpoint.
    /// </param>
    /// <returns>
    /// The identifier of the matching language.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="googleLanguageCode"/> is empty or consists
    /// only of white-space characters.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when <paramref name="googleLanguageCode"/> does not match any
    /// language known to GLTranslate.
    /// </exception>
    public static LanguageId FromGoogleCode(string googleLanguageCode)
    {
        return Instance.FromProviderCode(googleLanguageCode);
    }
}
