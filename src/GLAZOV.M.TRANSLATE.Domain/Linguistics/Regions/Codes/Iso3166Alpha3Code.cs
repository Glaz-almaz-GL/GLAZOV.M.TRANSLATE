using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Regions;

namespace GLAZOV.M.TRANSLATE.Domain.Linguistics.Regions.Codes;

/// <summary>
/// Represents an ISO 3166-1 alpha-3 region code.
/// </summary>
public sealed class Iso3166Alpha3Code(string value) : RegionCode(Normalize(value))
{
    private static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        value = value.Trim().ToUpperInvariant();

        if (value.Length != 3 || !value.All(char.IsAsciiLetter))
        {
            // ISO 3166-1 alpha-3 code must contain exactly three letters. (RUS, USA)
            throw new ArgumentException("ISO 3166-1 alpha-3 code must contain exactly three letters.", nameof(value));
        }

        return value;
    }
}