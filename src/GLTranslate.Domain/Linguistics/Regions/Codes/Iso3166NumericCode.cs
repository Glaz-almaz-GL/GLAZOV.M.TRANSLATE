using GLTranslate.Abstractions.Linguistics.Regions;

namespace GLTranslate.Domain.Linguistics.Regions.Codes;

/// <summary>
/// Represents an ISO 3166-1 numeric region code.
/// </summary>
public sealed class Iso3166NumericCode(string value) : RegionCode(Normalize(value))
{
    private static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        value = value.Trim();

        if (value.Length != 3 || !value.All(char.IsDigit))
        {
            // ISO 3166-1 numeric code must contain exactly three digits. (643, 840)
            throw new ArgumentException("ISO 3166-1 numeric code must contain exactly three digits.", nameof(value));
        }

        return value;
    }
}