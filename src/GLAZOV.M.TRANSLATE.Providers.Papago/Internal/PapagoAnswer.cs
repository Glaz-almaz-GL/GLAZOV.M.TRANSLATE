namespace GLAZOV.M.TRANSLATE.Providers.Papago.Internal;

/// <summary>
/// What Papago answered to one translation request.
/// </summary>
/// <param name="TranslatedText">The translation.</param>
/// <param name="SourceLanguageCode">
/// The code of the source language Papago used, or <see langword="null"/> when
/// it could not tell.
/// </param>
/// <param name="SourceTransliteration">
/// The source text written in the Latin script, or <see langword="null"/> when
/// Papago gave none, which it does not for text already in that script.
/// </param>
internal readonly record struct PapagoAnswer(
    string TranslatedText,
    string? SourceLanguageCode,
    string? SourceTransliteration);
