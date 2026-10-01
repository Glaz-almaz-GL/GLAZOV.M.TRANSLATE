namespace GLAZOV.M.TRANSLATE.Providers.Google.Internal;

/// <summary>
/// Knows which languages the Google speech endpoint can speak.
/// </summary>
/// <remarks>
/// The endpoint publishes no list of its own: it answers 200 for a language
/// it speaks and 400 for one it does not. The list is therefore found by
/// asking it language by language, which
/// tools/GLAZOV.M.TRANSLATE.Providers.Google.Generator does, and is checked here
/// before a request travels.
/// </remarks>
internal static partial class GoogleSpeechLanguages
{
    /// <summary>
    /// Determines whether the endpoint speaks the specified language.
    /// </summary>
    /// <param name="iso6391Code">
    /// The ISO 639-1 code of the language.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the endpoint speaks it;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool Contains(string iso6391Code)
    {
        return Spoken.Contains(iso6391Code);
    }
}
