using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Providers.Common;

namespace GLTranslate.Providers.Yandex.Internal;

/// <summary>
/// Resolves between GLTranslate <see cref="LanguageId"/> values and the
/// language codes the Yandex endpoints expect.
/// </summary>
/// <remarks>
/// Yandex speaks plain ISO 639-1 throughout, so this resolver adds nothing to
/// <see cref="LanguageCodeResolver"/> but the name of the provider.
/// </remarks>
internal static class YandexLanguageCodeResolver
{
    /// <summary>
    /// Gets how the languages of GLTranslate are written in the provider's terms.
    /// </summary>
    internal static LanguageCodeResolver Instance { get; } = new(YandexProvider.Name);

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
    public static string ToYandexCode(LanguageId languageId)
    {
        return Instance.ToProviderCode(languageId);
    }

    /// <summary>
    /// Converts a language code returned by a Yandex endpoint into the
    /// corresponding GLTranslate <see cref="LanguageId"/>.
    /// </summary>
    /// <param name="yandexLanguageCode">
    /// The language code returned by the endpoint.
    /// </param>
    /// <returns>
    /// The identifier of the matching language.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="yandexLanguageCode"/> is empty or consists
    /// only of white-space characters.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when <paramref name="yandexLanguageCode"/> does not match any
    /// language known to GLTranslate.
    /// </exception>
    public static LanguageId FromYandexCode(string yandexLanguageCode)
    {
        return Instance.FromProviderCode(yandexLanguageCode);
    }
}
