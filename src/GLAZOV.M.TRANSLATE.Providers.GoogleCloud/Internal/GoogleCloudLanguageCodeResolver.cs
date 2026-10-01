using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Providers.Common;

namespace GLAZOV.M.TRANSLATE.Providers.GoogleCloud.Internal;

/// <summary>
/// Translates between the languages of GLAZOV.M.TRANSLATE and the language codes of
/// Google Cloud.
/// </summary>
/// <remarks>
/// Google Cloud writes languages as their ISO 639-1 code, which is what
/// GLAZOV.M.TRANSLATE keeps, so nothing needs translating but the lookup.
/// </remarks>
internal static class GoogleCloudLanguageCodeResolver
{
    /// <summary>
    /// Gets how the languages of GLAZOV.M.TRANSLATE are written in the provider's terms.
    /// </summary>
    internal static LanguageCodeResolver Instance { get; } = new(GoogleCloudProvider.Name);

    /// <summary>
    /// Gets the Google Cloud code of a language.
    /// </summary>
    /// <param name="languageId">
    /// The language.
    /// </param>
    /// <returns>
    /// The code Google Cloud knows the language by.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="languageId"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="Abstractions.Providers.ProviderException">
    /// Thrown when the language is unknown to GLAZOV.M.TRANSLATE or has no ISO 639-1
    /// code.
    /// </exception>
    public static string ToGoogleCloudCode(LanguageId languageId)
    {
        return Instance.ToProviderCode(languageId);
    }

    /// <summary>
    /// Gets the language a Google Cloud code stands for.
    /// </summary>
    /// <param name="googleCloudLanguageCode">
    /// The code Google Cloud answered with, possibly with a region as in
    /// <c>zh-CN</c>, which is read as the language alone.
    /// </param>
    /// <returns>
    /// The language.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="googleCloudLanguageCode"/> is empty or
    /// consists only of white-space characters.
    /// </exception>
    /// <exception cref="Abstractions.Providers.ProviderException">
    /// Thrown when the code is not known to GLAZOV.M.TRANSLATE.
    /// </exception>
    public static LanguageId FromGoogleCloudCode(string googleCloudLanguageCode)
    {
        return Instance.FromProviderCode(googleCloudLanguageCode);
    }
}
