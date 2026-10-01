using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;

namespace GLAZOV.M.TRANSLATE.Domain.Linguistics.Languages.Codes;

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

        if (normalized.Length != 2 || !normalized.All(char.IsAsciiLetter))
        {
            // ISO 639-1 language code must contain exactly two Latin letters. (ru, en)
            throw new ArgumentException("ISO 639-1 language code must contain exactly two Latin letters.", nameof(value));
        }

        return normalized;
    }
}
