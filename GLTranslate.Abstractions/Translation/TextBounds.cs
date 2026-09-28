namespace GLTranslate.Abstractions.Translation;

/// <summary>
/// Represents where a piece of text sits on an image, in pixels from its
/// top-left corner.
/// </summary>
/// <param name="X">
/// The distance from the left edge of the image.
/// </param>
/// <param name="Y">
/// The distance from the top edge of the image.
/// </param>
/// <param name="Width">
/// The width of the piece of text.
/// </param>
/// <param name="Height">
/// The height of the piece of text.
/// </param>
public readonly record struct TextBounds(int X, int Y, int Width, int Height);
