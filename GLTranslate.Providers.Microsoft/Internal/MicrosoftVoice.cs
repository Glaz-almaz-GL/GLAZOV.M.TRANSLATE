namespace GLTranslate.Providers.Microsoft.Internal;

/// <summary>
/// Represents the voice the speech endpoint speaks a language with.
/// </summary>
/// <param name="ShortName">
/// The name the speech endpoint knows the voice by, such as
/// <c>ru-RU-SvetlanaNeural</c>.
/// </param>
/// <param name="Gender">
/// The gender the voice speaks with, as the speech endpoint names it.
/// </param>
/// <param name="Locale">
/// The locale of the voice, such as <c>ru-RU</c>.
/// </param>
internal readonly record struct MicrosoftVoice(string ShortName, string Gender, string Locale);
