using GLAZOV.M.TRANSLATE.Abstractions.Common;

namespace GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;

/// <summary>
/// Represents the unique identifier of a language within GLAZOV.M.TRANSLATE.
/// </summary>
/// <remarks>
/// A language identifier is independent from any external language coding
/// standard or translation provider.
/// </remarks>
public sealed class LanguageId(string value) : StringValueObject(value);