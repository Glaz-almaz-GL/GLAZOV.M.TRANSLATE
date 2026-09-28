using GLTranslate.Abstractions.Linguistics.Scripts;

namespace GLTranslate.Domain.Linguistics.Scripts.Code;

/// <summary>
/// Represents an ISO 15924 script code.
/// </summary>
/// <remarks>
/// ISO 15924 is the standard used to identify writing systems.
/// </remarks>
public sealed class Iso15924Code(string value) : ScriptCode(Normalize(value))
{
    private static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        value = value.Trim();

        if (value.Length != 4)
        {
            // ISO 15924 codes must consist of exactly four letters. (Latn, Cyrl)
            throw new ArgumentException("ISO 15924 codes must consist of exactly four letters.", nameof(value));
        }

        if (!value.All(char.IsAsciiLetter))
        {
            // ISO 15924 codes must consist of exactly four letters. (Latn and Cyrl, not Латин and Кирил)
            throw new ArgumentException("ISO 15924 codes may contain only Latin letters.", nameof(value));
        }

        return char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
    }
}