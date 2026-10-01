using GLAZOV.M.TRANSLATE.Abstractions.Common;

namespace GLAZOV.M.TRANSLATE.Abstractions.Transliteration;

/// <summary>
/// Represents a phonetic rendering of text that shows how it is
/// pronounced, typically using the Latin script.
/// </summary>
/// <remarks>
/// Instances of this class are immutable and thread-safe.
/// </remarks>
public sealed class TransliteratedText(string value) : StringValueObject(value);
