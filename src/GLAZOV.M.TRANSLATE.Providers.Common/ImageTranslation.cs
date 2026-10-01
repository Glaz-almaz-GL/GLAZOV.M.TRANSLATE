using GLAZOV.M.TRANSLATE.Abstractions.Translation;
using System.Collections.Immutable;

namespace GLAZOV.M.TRANSLATE.Providers.Common;

/// <summary>
/// Represents what an engine read on an image and translated.
/// </summary>
/// <param name="Lines">
/// The lines read, each with its translation and where it sits. An image with
/// no text on it yields none.
/// </param>
/// <param name="SourceLanguageCode">
/// The code, in the provider's own terms, of the language the text was found to
/// be in, or <see langword="null"/> when the provider reports none.
/// </param>
public readonly record struct ImageTranslation(ImmutableArray<TranslatedLine> Lines, string? SourceLanguageCode);
