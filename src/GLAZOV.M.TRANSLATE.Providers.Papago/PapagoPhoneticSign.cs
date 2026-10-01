namespace GLAZOV.M.TRANSLATE.Providers.Papago;

/// <summary>
/// How a word is pronounced, written in phonetic signs.
/// </summary>
/// <param name="Variety">
/// The variety of the language the pronunciation belongs to, as the dictionary
/// names it (in the language of the dictionary), or an empty string when it
/// names none.
/// </param>
/// <param name="Sign">The pronunciation in phonetic signs.</param>
/// <remarks>
/// Instances are immutable and therefore thread-safe.
/// </remarks>
public sealed record PapagoPhoneticSign(string Variety, string Sign);