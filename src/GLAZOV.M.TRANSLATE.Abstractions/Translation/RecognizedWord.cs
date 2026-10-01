namespace GLAZOV.M.TRANSLATE.Abstractions.Translation;

/// <summary>
/// Represents one word a provider read on an image.
/// </summary>
/// <param name="Text">
/// The word as it was read.
/// </param>
/// <param name="Bounds">
/// Where the word sits on the image.
/// </param>
public readonly record struct RecognizedWord(string Text, TextBounds Bounds);
