namespace GLAZOV.M.TRANSLATE.Providers.Common;

/// <summary>
/// Represents what an engine translated and, when the request left it to
/// detect the language, which language it found.
/// </summary>
/// <param name="Text">
/// The translation: plain text or markup, according to what was translated.
/// </param>
/// <param name="DetectedSourceLanguageCode">
/// The code, in the provider's own terms, of the language the engine found the
/// original to be in, or <see langword="null"/> when it reports none.
/// </param>
public readonly record struct ProviderTranslation(string Text, string? DetectedSourceLanguageCode);
