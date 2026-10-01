namespace GLAZOV.M.TRANSLATE.Providers.Google;

/// <summary>
/// A phrase Google Translate suggests to complete what is being typed, with its
/// translation.
/// </summary>
/// <param name="Phrase">The suggested phrase, in the source language.</param>
/// <param name="Translation">The translation of <paramref name="Phrase"/>.</param>
/// <remarks>
/// Instances are immutable and therefore thread-safe.
/// </remarks>
public sealed record GoogleSuggestion(string Phrase, string Translation);
