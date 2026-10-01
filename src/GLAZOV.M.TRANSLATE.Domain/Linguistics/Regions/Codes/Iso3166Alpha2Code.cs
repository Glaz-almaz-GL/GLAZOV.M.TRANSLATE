using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Regions;

namespace GLAZOV.M.TRANSLATE.Domain.Linguistics.Regions.Codes;

/// <summary>
/// Represents an ISO 3166-1 alpha-2 region code.
/// </summary>
public sealed class Iso3166Alpha2Code(string value) : RegionCode(Normalize(value))
{
    private static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        value = value.Trim().ToUpperInvariant();

        if (value.Length != 2 || !value.All(char.IsAsciiLetter))
        {
            // ISO 3166-1 alpha-2 code must contain exactly two letters. (RU, US)
            throw new ArgumentException("ISO 3166-1 alpha-2 code must contain exactly two letters.", nameof(value));
        }

        return value;
    }
}