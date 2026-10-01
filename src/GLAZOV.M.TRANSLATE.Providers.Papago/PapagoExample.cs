namespace GLAZOV.M.TRANSLATE.Providers.Papago;

/// <summary>
/// A sentence from the dictionary that shows a word in use, with its translation.
/// </summary>
/// <param name="Text">The sentence, in the language of the word.</param>
/// <param name="Translation">The translation of <paramref name="Text"/>.</param>
/// <remarks>
/// Instances are immutable and therefore thread-safe.
/// </remarks>
public sealed record PapagoExample(string Text, string Translation);