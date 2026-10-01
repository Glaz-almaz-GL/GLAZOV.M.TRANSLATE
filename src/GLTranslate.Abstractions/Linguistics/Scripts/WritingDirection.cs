namespace GLTranslate.Abstractions.Linguistics.Scripts;

/// <summary>
/// Specifies the direction in which a writing system is written.
/// </summary>
/// <remarks>
/// Writing direction is a property of the writing system, not of the language:
/// a language written in several scripts inherits the direction of each of them.
/// </remarks>
public enum WritingDirection
{
    /// <summary>
    /// The writing system is written from left to right.
    /// </summary>
    LeftToRight,

    /// <summary>
    /// The writing system is written from right to left.
    /// </summary>
    RightToLeft
}
