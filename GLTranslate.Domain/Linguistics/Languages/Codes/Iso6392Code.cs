using GLTranslate.Abstractions.Linguistics.Languages;

namespace GLTranslate.Domain.Linguistics.Languages.Codes;

/// <summary>
/// Represents an ISO 639-2 language code.
/// </summary>
public sealed class Iso6392Code(string value) : LanguageCode(Normalize(value))
{
    private static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        string normalized = value
            .Trim()
            .ToLowerInvariant();

        if (normalized.Length != 3)
        {
            // ISO 639-2 language code must contain exactly three characters. (rus, eng)
            throw new ArgumentException("ISO 639-2 language code must contain exactly three characters.", nameof(value));
        }

        return normalized;
    }
}
