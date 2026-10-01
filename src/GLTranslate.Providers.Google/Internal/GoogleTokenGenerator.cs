namespace GLTranslate.Providers.Google.Internal;

/// <summary>
/// Generates the <c>tk</c> query parameter required by the free Google
/// Translate web endpoint.
/// </summary>
/// <remarks>
/// <para>
/// This is a reimplementation of an undocumented, reverse-engineered
/// algorithm used by Google's public translation widget. It is not part
/// of any official API and may stop working, or start producing rejected
/// tokens, without notice if Google changes it.
/// </para>
/// <para>
/// The token only depends on the text and the current hour (UTC); unlike
/// the older algorithm, it requires no extra network round-trip to fetch a
/// rotating key from the Google Translate home page.
/// </para>
/// </remarks>
internal static class GoogleTokenGenerator
{
    private const string Salt1 = "+-a^+6";
    private const string Salt2 = "+-3^+b+-f";

    /// <summary>
    /// Generates the <c>tk</c> token for the specified text.
    /// </summary>
    /// <param name="text">
    /// The text the token is computed for.
    /// </param>
    /// <returns>
    /// The token value expected by the Google Translate web endpoint.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text"/> is <see langword="null"/>.
    /// </exception>
    public static string Generate(string text)
    {
        return Generate(text, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Generates the <c>tk</c> token for the specified text as of the
    /// specified timestamp.
    /// </summary>
    /// <param name="text">
    /// The text the token is computed for.
    /// </param>
    /// <param name="timestamp">
    /// The timestamp used to derive the token's time component. Exposed
    /// separately from <see cref="Generate(string)"/> so the algorithm's
    /// output can be verified deterministically in tests.
    /// </param>
    /// <returns>
    /// The token value expected by the Google Translate web endpoint.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text"/> is <see langword="null"/>.
    /// </exception>
    public static string Generate(string text, DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (timestamp == DateTimeOffset.MinValue || timestamp == DateTimeOffset.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(timestamp), timestamp, "Timestamp must be a valid date and time.");
        }

        long time = timestamp.ToUnixTimeSeconds() / 3600;
        long seed = time;

        foreach (char character in text)
        {
            time = Scramble(time + character, Salt1);
        }

        time = Scramble(time, Salt2);

        if (time < 0)
        {
            time = (time & int.MaxValue) + int.MaxValue + 1;
        }

        time %= 1_000_000;

        return $"{time}.{time ^ seed}";
    }

    /// <summary>
    /// Scrambles the specified value using the specified salt string.
    /// </summary>
    /// <param name="value">The value to scramble.</param>
    /// <param name="salt">The salt string.</param>
    /// <returns>The scrambled value.</returns>
    private static long Scramble(long value, string salt)
    {
        for (int i = 0; i < salt.Length - 2; i += 3)
        {
            int shift = salt[i + 2];

            if (shift >= 'a')
            {
                shift -= 'W';
            }

            value = salt[i + 1] == '+'
                ? (value + (value >> shift)) & uint.MaxValue
                : value ^ (value << shift);
        }

        return value;
    }
}
