using System.Collections.Immutable;

namespace GLAZOV.M.TRANSLATE.Providers.Papago;

/// <summary>
/// The meanings of a word that belong to one part of speech.
/// </summary>
/// <param name="Name">
/// The name of the part of speech as the dictionary writes it, such as
/// <c>Noun</c> or <c>Intransitive verb, Transitive verb</c>.
/// </param>
/// <param name="Meanings">The meanings, in the order of the dictionary.</param>
/// <remarks>
/// Instances are immutable and therefore thread-safe.
/// </remarks>
public sealed record PapagoPartOfSpeech(string Name, ImmutableArray<PapagoMeaning> Meanings);