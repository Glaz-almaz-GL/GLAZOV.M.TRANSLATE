using System.Diagnostics.CodeAnalysis;

namespace GLTranslate.Providers.YandexCloud.Internal;

/// <summary>
/// Knows which languages the SpeechKit v1 speaks and in which voice it speaks
/// them when the request names none.
/// </summary>
/// <remarks>
/// SpeechKit takes a language as a code with a region and needs a voice of
/// that language, and it has voices for only a few languages. The table is
/// that of Yandex's list of voices available to the v1 API; Hebrew, for one,
/// has a voice in the newer API only.
/// </remarks>
internal static class YandexCloudSpeechVoices
{
    // ISO 639-1 code -> language code with region, and the voice that speaks it
    // by default.
    private static readonly Dictionary<string, (string Language, string Voice)> Spoken = new(StringComparer.Ordinal)
    {
        ["ru"] = ("ru-RU", "marina"),
        ["en"] = ("en-US", "john"),
        ["de"] = ("de-DE", "lea"),
        ["kk"] = ("kk-KK", "amira"),
        ["uz"] = ("uz-UZ", "nigora"),
    };

    /// <summary>
    /// Finds how a language is spoken.
    /// </summary>
    /// <param name="iso6391Code">
    /// The ISO 639-1 code of the language.
    /// </param>
    /// <param name="language">
    /// The code of the language with its region, as SpeechKit takes it.
    /// </param>
    /// <param name="voice">
    /// The voice that speaks the language by default.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when SpeechKit speaks the language;
    /// otherwise <see langword="false"/>.
    /// </returns>
    public static bool TryGet(string iso6391Code, [NotNullWhen(true)] out string? language, [NotNullWhen(true)] out string? voice)
    {
        if (Spoken.TryGetValue(iso6391Code, out (string Language, string Voice) found))
        {
            (language, voice) = found;

            return true;
        }

        language = null;
        voice = null;

        return false;
    }
}
