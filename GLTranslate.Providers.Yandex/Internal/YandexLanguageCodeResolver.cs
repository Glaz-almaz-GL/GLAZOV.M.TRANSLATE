using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Domain.Linguistics.Languages;
using GLTranslate.Domain.Linguistics.Languages.Codes;
using System.Collections.Immutable;

namespace GLTranslate.Providers.Yandex.Internal;

/// <summary>
/// Resolves between GLTranslate <see cref="LanguageId"/> values and the
/// language codes the Yandex endpoints expect.
/// </summary>
/// <remarks>
/// This resolver is provider-specific: it only exists so the Yandex providers
/// can cross the boundary between the standard-independent domain model and
/// the ISO-639-1-based wire format of the endpoints.
/// </remarks>
internal static class YandexLanguageCodeResolver
{
    private static readonly Lazy<ImmutableDictionary<string, LanguageId>> LanguagesByIso6391 = new(BuildIndex);

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
        ArgumentNullException.ThrowIfNull(languageId);

        Language language;

        try
        {
            language = LanguageRegistry.Default.Get(languageId);
        }
        catch (KeyNotFoundException exception)
        {
            throw new ProviderException(
                YandexProvider.Name,
                $"Language '{languageId.Value}' is not known to GLTranslate.",
                exception);
        }

        if (!language.Codes.TryGetValue(out Iso6391Code? code))
        {
            // The language is known to GLTranslate, but it has no ISO 639-1 code.
            throw new ProviderException(
                YandexProvider.Name,
                $"Language '{languageId.Value}' has no ISO 639-1 code, which Yandex requires.");
        }

        return code.Value;
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
        ArgumentException.ThrowIfNullOrWhiteSpace(yandexLanguageCode);

        if (!LanguagesByIso6391.Value.TryGetValue(yandexLanguageCode.Trim().ToLowerInvariant(), out LanguageId? languageId))
        {
            // The endpoint returned a language code that is not known to GLTranslate.
            throw new ProviderException(
                YandexProvider.Name,
                $"Yandex returned an unknown language code '{yandexLanguageCode}'.");
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
