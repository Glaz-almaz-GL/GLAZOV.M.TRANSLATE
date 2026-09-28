using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Domain.Linguistics.Languages;
using GLTranslate.Domain.Linguistics.Languages.Codes;
using System.Collections.Immutable;

namespace GLTranslate.Providers.Google.Internal;

/// <summary>
/// Resolves between GLTranslate <see cref="LanguageId"/> values and the
/// ISO 639-1 codes the Google Translate web endpoint expects.
/// </summary>
/// <remarks>
/// This resolver is provider-specific: it only exists so
/// <see cref="GoogleTranslationProvider"/> can cross the boundary between
/// the standard-independent domain model and Google's ISO-639-1-based
/// wire format.
/// </remarks>
internal static class GoogleLanguageCodeResolver
{
    private const string AutoDetectCode = "auto";

    private static readonly Lazy<ImmutableDictionary<string, LanguageId>> LanguagesByIso6391 = new(BuildIndex);

    /// <summary>
    /// Converts a <see cref="LanguageId"/> into the ISO 639-1 code Google
    /// Translate expects.
    /// </summary>
    /// <param name="languageId">
    /// The language identifier, or <see langword="null"/> to request
    /// automatic source language detection.
    /// </param>
    /// <returns>
    /// The ISO 639-1 code, or <c>"auto"</c> when <paramref name="languageId"/>
    /// is <see langword="null"/>.
    /// </returns>
    /// <exception cref="ProviderException">
    /// Thrown when <paramref name="languageId"/> is not known to GLTranslate,
    /// or when the resolved language has no ISO 639-1 code.
    /// </exception>
    public static string ToGoogleCode(LanguageId? languageId)
    {
        if (languageId is null)
        {
            return AutoDetectCode;
        }

        Language language;

        try
        {
            language = LanguageRegistry.Default.Get(languageId);
        }
        catch (KeyNotFoundException exception)
        {
            throw new ProviderException(
                GoogleProvider.Name,
                $"Language '{languageId.Value}' is not known to GLTranslate.",
                exception);
        }

        if (!language.Codes.TryGetValue(out Iso6391Code? code))
        {
            // The language is known to GLTranslate, but it has no ISO 639-1 code.
            throw new ProviderException(
                GoogleProvider.Name,
                $"Language '{languageId.Value}' has no ISO 639-1 code, which Google Translate requires.");
        }

        return code.Value;
    }

    /// <summary>
    /// Converts an ISO 639-1 code returned by Google Translate into the
    /// corresponding GLTranslate <see cref="LanguageId"/>.
    /// </summary>
    /// <param name="googleLanguageCode">
    /// The ISO 639-1 code returned by the Google Translate web endpoint.
    /// </param>
    /// <returns>
    /// The identifier of the matching language.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="googleLanguageCode"/> is empty or
    /// consists only of white-space characters.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when <paramref name="googleLanguageCode"/> does not match any
    /// language known to GLTranslate.
    /// </exception>
    public static LanguageId FromGoogleCode(string googleLanguageCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(googleLanguageCode);

        if (!LanguagesByIso6391.Value.TryGetValue(googleLanguageCode.ToLowerInvariant(), out LanguageId? languageId))
        {
            // The Google Translate endpoint returned a language code that is not known to GLTranslate.
            throw new ProviderException(
                GoogleProvider.Name,
                $"Google Translate returned an unknown language code '{googleLanguageCode}'.");
        }

        return languageId;
    }

    /// <summary>
    /// Builds an index of ISO 639-1 codes to GLTranslate <see cref="LanguageId"/> values.
    /// </summary>
    /// <returns>The immutable dictionary mapping ISO 639-1 codes to language identifiers.</returns>
    private static ImmutableDictionary<string, LanguageId> BuildIndex()
    {
        ImmutableDictionary<string, LanguageId>.Builder index = ImmutableDictionary.CreateBuilder<string, LanguageId>();

        foreach (Language language in LanguageRegistry.Default.All)
        {
            if (language.Codes.TryGetValue(out Iso6391Code? code))
            {
                index[code.Value] = language.Id;
            }
        }

        return index.ToImmutableDictionary();
    }
}
