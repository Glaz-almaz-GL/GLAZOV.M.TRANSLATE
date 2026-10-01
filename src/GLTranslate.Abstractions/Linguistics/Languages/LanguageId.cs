using GLTranslate.Abstractions.Common;

namespace GLTranslate.Abstractions.Linguistics.Languages;

/// <summary>
/// Represents the unique identifier of a language within GLTranslate.
/// </summary>
/// <remarks>
/// A language identifier is independent from any external language coding
/// standard or translation provider.
/// </remarks>
public sealed class LanguageId(string value) : StringValueObject(value);