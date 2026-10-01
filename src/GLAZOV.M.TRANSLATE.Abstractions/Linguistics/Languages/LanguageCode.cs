using GLAZOV.M.TRANSLATE.Abstractions.Common;
using GLAZOV.M.TRANSLATE.Abstractions.Interfaces;

namespace GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;

/// <summary>
/// Represents the base class for all language code representations.
/// </summary>
/// <remarks>
/// Different implementations represent different language coding systems,
/// such as ISO 639 or BCP-47.
/// </remarks>
public abstract class LanguageCode(string value) : StringValueObject(value), ICode;