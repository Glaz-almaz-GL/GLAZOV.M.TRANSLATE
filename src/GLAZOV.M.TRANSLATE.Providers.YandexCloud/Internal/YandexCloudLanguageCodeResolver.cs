using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Providers.Common;

namespace GLAZOV.M.TRANSLATE.Providers.YandexCloud.Internal;

/// <summary>
/// Translates between the languages of GLAZOV.M.TRANSLATE and the language codes of
/// Yandex Cloud.
/// </summary>
/// <remarks>
/// Yandex Cloud writes languages as their ISO 639-1 code, which is what
/// GLAZOV.M.TRANSLATE keeps, so nothing needs translating but the lookup.
/// </remarks>
internal static class YandexCloudLanguageCodeResolver
{
    /// <summary>
    /// Gets how the languages of GLAZOV.M.TRANSLATE are written in the provider's terms.
    /// </summary>
    internal static LanguageCodeResolver Instance { get; } = new(YandexCloudProvider.Name);

    /// <summary>
    /// Gets the Yandex Cloud code of a language.
    /// </summary>
    /// <param name="languageId">
    /// The language.
    /// </param>
    /// <returns>
    /// The code Yandex Cloud knows the language by.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="languageId"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="Abstractions.Providers.ProviderException">
    /// Thrown when the language is unknown to GLAZOV.M.TRANSLATE or has no ISO 639-1
    /// code.
    /// </exception>
    public static string ToYandexCloudCode(LanguageId languageId)
    {
        return Instance.ToProviderCode(languageId);
    }

    /// <summary>
    /// Gets the language a Yandex Cloud code stands for.
    /// </summary>
    /// <param name="yandexCloudLanguageCode">
    /// The code Yandex Cloud answered with.
    /// </param>
    /// <returns>
    /// The language.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="yandexCloudLanguageCode"/> is empty or
    /// consists only of white-space characters.
    /// </exception>
    /// <exception cref="Abstractions.Providers.ProviderException">
    /// Thrown when the code is not known to GLAZOV.M.TRANSLATE.
    /// </exception>
    public static LanguageId FromYandexCloudCode(string yandexCloudLanguageCode)
    {
        return Instance.FromProviderCode(yandexCloudLanguageCode);
    }
}
