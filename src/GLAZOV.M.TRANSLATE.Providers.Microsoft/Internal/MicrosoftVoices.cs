namespace GLAZOV.M.TRANSLATE.Providers.Microsoft.Internal;

/// <summary>
/// Holds the voice the Microsoft speech endpoint speaks every language with.
/// </summary>
/// <remarks>
/// The endpoint speaks a named voice, not a language, so a language has to be
/// turned into one. The table is generated from the live list of voices by
/// tools/GLAZOV.M.TRANSLATE.Providers.Microsoft.Generator: of the voices of a
/// language it keeps the first that is generally available and neural,
/// preferring the locale whose region repeats the language.
/// </remarks>
internal static partial class MicrosoftVoices
{
    /// <summary>
    /// Attempts to get the default voice of the specified language.
    /// </summary>
    /// <param name="iso6391Code">
    /// The ISO 639-1 code of the language.
    /// </param>
    /// <param name="voiceName">
    /// When this method returns <see langword="true"/>, contains the name of
    /// the voice the endpoint speaks the language with.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the endpoint speaks the language;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool TryGetDefault(string iso6391Code, out string voiceName)
    {
        return DefaultByIso6391.TryGetValue(iso6391Code, out voiceName!);
    }

    /// <summary>
    /// Gets the locale a voice belongs to, which its name begins with.
    /// </summary>
    /// <param name="voiceName">
    /// The name of the voice, such as <c>ru-RU-DariyaNeural</c>.
    /// </param>
    /// <returns>
    /// The locale of the voice, such as <c>ru-RU</c>, or the whole name when
    /// it does not carry one.
    /// </returns>
    /// <remarks>
    /// A name is its locale followed by the name of the voice itself, so the
    /// locale is everything but the last part. A locale may name a script as
    /// well as a region, as in <c>sr-Latn-RS-NicholasNeural</c>.
    /// </remarks>
    public static string GetLocale(string voiceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(voiceName);

        int lastSeparator = voiceName.LastIndexOf('-');

        return lastSeparator > 0 ? voiceName[..lastSeparator] : voiceName;
    }
}
