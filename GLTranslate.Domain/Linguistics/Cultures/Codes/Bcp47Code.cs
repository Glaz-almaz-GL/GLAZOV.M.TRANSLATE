using GLTranslate.Abstractions.Linguistics.Cultures;

namespace GLTranslate.Domain.Linguistics.Cultures.Codes;

/// <summary>
/// Represents a culture code defined by the
/// <c>BCP 47</c> (Best Current Practice 47) specification.
/// </summary>
/// <remarks>
/// <para>
/// BCP 47 is the most widely used standard for identifying languages,
/// scripts, regions and their combinations.
/// </para>
/// <para>
/// Examples of valid BCP 47 codes include:
/// <list type="bullet">
/// <item><c>en</c></item>
/// <item><c>en-US</c></item>
/// <item><c>ru</c></item>
/// <item><c>ru-KZ</c></item>
/// <item><c>zh-Hans</c></item>
/// <item><c>zh-Hant-TW</c></item>
/// <item><c>en-US-POSIX</c></item>
/// </list>
/// </para>
/// <para>
/// The supported shape is a primary language subtag, optionally followed
/// by a script subtag, a region subtag and variant subtags. Extension and
/// private-use subtags, which BCP 47 also allows, are not accepted.
/// </para>
/// <para>
/// The language subtag is normalized to lower case, the script subtag to
/// title case and the region subtag to upper case, as the specification
/// recommends. Variant subtags are kept as given.
/// </para>
/// <para>
/// Instances of <see cref="Bcp47Code"/> are immutable and thread-safe.
/// </para>
/// </remarks>
/// <param name="value">
/// The textual representation of the BCP 47 culture code.
/// </param>
/// <exception cref="ArgumentNullException">
/// Thrown when <paramref name="value"/> is <see langword="null"/>.
/// </exception>
/// <exception cref="ArgumentException">
/// Thrown when <paramref name="value"/> is empty, contains only
/// white-space characters, or is not a well-formed BCP 47 code of the
/// supported shape.
/// </exception>
public sealed class Bcp47Code(string value) : CultureCode(Normalize(value))
{
    /// <summary>
    /// Normalizes the specified BCP 47 code.
    /// </summary>
    /// <param name="value">
    /// The code to normalize.
    /// </param>
    /// <returns>
    /// A normalized representation of the specified BCP 47 code.
    /// </returns>
    private static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        string[] subtags = value.Trim().Split('-');

        if (!IsLanguage(subtags[0]))
        {
            // The first subtag is always the primary language. (en, ru, gsw)
            throw new ArgumentException(
                "A BCP 47 culture code must start with a language subtag of two to eight Latin letters.",
                nameof(value));
        }

        subtags[0] = subtags[0].ToLowerInvariant();

        int index = 1;

        if (index < subtags.Length && IsScript(subtags[index]))
        {
            subtags[index] = char.ToUpperInvariant(subtags[index][0]) + subtags[index][1..].ToLowerInvariant();
            index++;
        }

        if (index < subtags.Length && IsRegion(subtags[index]))
        {
            subtags[index] = subtags[index].ToUpperInvariant();
            index++;
        }

        for (; index < subtags.Length; index++)
        {
            if (!IsVariant(subtags[index]))
            {
                // Extension and private-use subtags are outside the supported shape.
                throw new ArgumentException(
                    $"'{subtags[index]}' is not a script, region or variant subtag of a BCP 47 culture code.",
                    nameof(value));
            }
        }

        return string.Join('-', subtags);
    }

    private static bool IsLanguage(string subtag)
    {
        return subtag.Length is >= 2 and <= 8 && subtag.All(char.IsAsciiLetter);
    }

    private static bool IsScript(string subtag)
    {
        return subtag.Length == 4 && subtag.All(char.IsAsciiLetter);
    }

    private static bool IsRegion(string subtag)
    {
        return (subtag.Length == 2 && subtag.All(char.IsAsciiLetter))
            || (subtag.Length == 3 && subtag.All(char.IsAsciiDigit));
    }

    private static bool IsVariant(string subtag)
    {
        // RFC 5646: five to eight alphanumerics, or four characters starting with a digit.
        return (subtag.Length is >= 5 and <= 8 && subtag.All(char.IsAsciiLetterOrDigit))
            || (subtag.Length == 4 && char.IsAsciiDigit(subtag[0]) && subtag.All(char.IsAsciiLetterOrDigit));
    }
}
