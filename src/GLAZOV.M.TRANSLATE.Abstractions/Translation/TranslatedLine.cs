using System.Collections.Immutable;

namespace GLAZOV.M.TRANSLATE.Abstractions.Translation;

/// <summary>
/// Represents one line a provider read on an image, and its translation.
/// </summary>
/// <param name="RecognizedText">
/// The line as it was read from the image.
/// </param>
/// <param name="TranslatedText">
/// The line in the language of the request.
/// </param>
/// <param name="Bounds">
/// Where the line sits on the image, which is what lets a caller draw the
/// translation over it.
/// </param>
/// <param name="Words">
/// The words of the line and where each of them sits. A provider that reports
/// no words leaves this empty; the words are of the line as it was read, not
/// of its translation, which need not have the same number of them.
/// </param>
public readonly record struct TranslatedLine(
    string RecognizedText,
    string TranslatedText,
    TextBounds Bounds,
    ImmutableArray<RecognizedWord> Words);
