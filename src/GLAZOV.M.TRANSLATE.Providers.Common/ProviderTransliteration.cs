namespace GLAZOV.M.TRANSLATE.Providers.Common;

/// <summary>
/// Represents what an engine transliterated and, when the language was left to
/// it, which language it found.
/// </summary>
/// <param name="Text">
/// The text in the Latin script.
/// </param>
/// <param name="DetectedLanguageCode">
/// The code, in the provider's own terms, of the language the engine found the
/// original to be in, or <see langword="null"/> when it reports none.
/// </param>
public readonly record struct ProviderTransliteration(string Text, string? DetectedLanguageCode);
