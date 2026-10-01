using GLAZOV.M.TRANSLATE.Providers.Common;

namespace GLAZOV.M.TRANSLATE.Providers.Papago.Internal;

/// <summary>
/// Resolves between the languages of the domain model and the language codes
/// of Papago.
/// </summary>
internal static class PapagoLanguageCodeResolver
{
    /// <summary>
    /// The code that asks Papago to detect the source language.
    /// </summary>
    internal const string AutoDetectCode = "auto";

    /// <summary>
    /// The code Papago answers with when it could not tell the language.
    /// </summary>
    internal const string UnknownCode = "unk";

    /// <summary>
    /// Gets the resolver shared by every Papago provider.
    /// </summary>
    internal static LanguageCodeResolver Instance { get; } = new(
        PapagoProvider.Name,
        [
            // Papago has no plain "zh": it asks which Chinese. The domain model
            // writes Chinese in the simplified script, so that is what is sent.
            new("zh", "zh-CN"),
        ],
        [
            // Papago writes Chinese split by script and Burmese as "mm".
            new("zh-CN", "zh"),
            new("zh-TW", "zh"),
            new("mm", "my"),
        ]);
}
