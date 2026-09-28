namespace GLTranslate.Providers.Bing.Internal;

/// <summary>
/// Represents what one call to the Bing Translator endpoint returned.
/// </summary>
/// <param name="TranslatedText">
/// The translated text.
/// </param>
/// <param name="SourceLanguageCode">
/// The code of the language the text was translated from.
/// </param>
/// <param name="InputTransliteration">
/// The source text in the Latin script, or <see langword="null"/> when the
/// endpoint did not render it - which it does only for a source written in a
/// script it can romanize.
/// </param>
internal readonly record struct BingAnswer(
    string TranslatedText,
    string SourceLanguageCode,
    string? InputTransliteration);
