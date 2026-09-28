using GLTranslate.Abstractions.Linguistics.Languages;

namespace GLTranslate.Domain.Linguistics.Languages.Codes;

/// <summary>
/// Represents an ISO 639-1 language code.
/// </summary>
public sealed class Iso6391Code(string value) : LanguageCode(Normalize(value))
{
    private static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        string normalized = value
            .Trim()
            .ToLowerInvariant();

        if (normalized.Length != 2)
        {
            // ISO 639-1 language code must contain exactly two characters. (ru, en)
            throw new ArgumentException("ISO 639-1 language code must contain exactly two characters.", nameof(value));
        }

        return normalized;
    }
}
