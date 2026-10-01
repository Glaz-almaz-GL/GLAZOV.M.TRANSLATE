using System.Collections.Immutable;

namespace GLAZOV.M.TRANSLATE.Providers.Papago;

/// <summary>
/// An entry of the Naver dictionary behind Papago: one headword with its
/// pronunciation and its meanings by part of speech.
/// </summary>
/// <param name="Headword">The word the entry describes, without the markup the service puts around the matched letters.</param>
/// <param name="PhoneticSigns">How the word is pronounced; empty when the dictionary gives no pronunciation.</param>
/// <param name="PartsOfSpeech">The meanings grouped by part of speech.</param>
/// <param name="DictionaryName">The name of the dictionary the entry comes from, or an empty string when the service names none.</param>
/// <param name="Address">The address of the entry on the dictionary's web site, or <see langword="null"/> when the service gives none.</param>
/// <remarks>
/// Instances are immutable and therefore thread-safe.
/// </remarks>
public sealed record PapagoDictionaryEntry(
    string Headword,
    ImmutableArray<PapagoPhoneticSign> PhoneticSigns,
    ImmutableArray<PapagoPartOfSpeech> PartsOfSpeech,
    string DictionaryName,
    Uri? Address);