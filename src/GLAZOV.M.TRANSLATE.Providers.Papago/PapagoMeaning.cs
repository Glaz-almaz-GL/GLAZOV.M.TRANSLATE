using System.Collections.Immutable;

namespace GLAZOV.M.TRANSLATE.Providers.Papago;

/// <summary>
/// One meaning of a word, as the dictionary gives it.
/// </summary>
/// <param name="Text">The meaning, written in the language the word is looked up into.</param>
/// <param name="Examples">Sentences that show the meaning in use; empty when the dictionary has none.</param>
/// <remarks>
/// Instances are immutable and therefore thread-safe.
/// </remarks>
public sealed record PapagoMeaning(string Text, ImmutableArray<PapagoExample> Examples);