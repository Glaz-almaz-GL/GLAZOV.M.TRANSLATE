namespace GLAZOV.M.TRANSLATE.Abstractions.Translation;

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
public readonly record struct TextBounds(int X, int Y, int Width, int Height)
{
    /// <summary>
    /// Makes the smallest upright rectangle that holds all the given points, which
    /// is how a provider's polygon around a piece of text becomes a place on the
    /// image.
    /// </summary>
    /// <param name="points">
    /// The corners of the polygon, in pixels from the top-left corner of the image.
    /// </param>
    /// <returns>
    /// The rectangle, or a rectangle of no size at the top-left corner when there
    /// are no points.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="points"/> is <see langword="null"/>.
    /// </exception>
    public static TextBounds Enclosing(IEnumerable<(int X, int Y)> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        bool any = false;
        int left = int.MaxValue;
        int top = int.MaxValue;
        int right = int.MinValue;
        int bottom = int.MinValue;

        foreach ((int x, int y) in points)
        {
            any = true;
            left = Math.Min(left, x);
            top = Math.Min(top, y);
            right = Math.Max(right, x);
            bottom = Math.Max(bottom, y);
        }

        return any ? new TextBounds(left, top, right - left, bottom - top) : default;
    }
}
