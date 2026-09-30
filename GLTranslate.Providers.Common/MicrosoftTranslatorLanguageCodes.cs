namespace GLTranslate.Providers.Common;

/// <summary>
/// Knows how the Microsoft translation service writes languages, which is how
/// every provider built on it does: Microsoft's own and Bing's.
/// </summary>
/// <remarks>
/// Both providers speak to the same service, so the table that says how a
/// language is written is kept once, here, and each provider makes a resolver of
/// its own from it, under its own name.
/// </remarks>
public static class MicrosoftTranslatorLanguageCodes
{
    /// <summary>
    /// Makes the resolver of a provider built on the Microsoft translation
    /// service.
    /// </summary>
    /// <param name="providerName">
    /// The name of the provider, which every failure of the resolver carries.
    /// </param>
    /// <returns>
    /// A resolver that writes languages the way Microsoft does: naming the
    /// script for the languages written in more than one, and using the
    /// ISO 639-3 code for a few.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="providerName"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="providerName"/> is empty or consists only of
    /// white-space characters.
    /// </exception>
    public static LanguageCodeResolver CreateResolver(string providerName)
    {
        return new LanguageCodeResolver(
            providerName,
            [
                new("lg", "lug"),
                new("no", "nb"),
                new("ny", "nya"),
                new("rn", "run"),
                new("mn", "mn-Cyrl"),
                new("sr", "sr-Cyrl"),
                new("zh", "zh-Hans"),
            ],
            [
                new("lug", "lg"),
                new("nb", "no"),
                new("nn", "no"),
                new("nya", "ny"),
                new("run", "rn"),
                new("mn-Cyrl", "mn"),
                new("mn-Mong", "mn"),
                new("sr-Cyrl", "sr"),
                new("sr-Latn", "sr"),
                new("zh-Hans", "zh"),
                new("zh-Hant", "zh"),
            ]);
    }
}
