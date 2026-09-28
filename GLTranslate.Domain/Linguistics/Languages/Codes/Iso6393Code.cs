using GLTranslate.Abstractions.Linguistics.Languages;

namespace GLTranslate.Domain.Linguistics.Languages.Codes;

/// <summary>
/// Represents an ISO 639-3 language code.
/// </summary>
public sealed class Iso6393Code(string value) : LanguageCode(Normalize(value))
{
    private static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        string normalized = value
            .Trim()
            .ToLowerInvariant();

        if (normalized.Length != 3 || !normalized.All(char.IsAsciiLetter))
        {
            // ISO 639-3 language code must contain exactly three Latin letters. (rus, eng)
            throw new ArgumentException("ISO 639-3 language code must contain exactly three Latin letters.", nameof(value));
        }

        return normalized;
    }
}
