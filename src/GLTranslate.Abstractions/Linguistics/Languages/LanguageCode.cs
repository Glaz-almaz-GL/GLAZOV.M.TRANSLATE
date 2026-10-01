using GLTranslate.Abstractions.Common;
using GLTranslate.Abstractions.Interfaces;

namespace GLTranslate.Abstractions.Linguistics.Languages;

/// <summary>
/// Represents the base class for all language code representations.
/// </summary>
/// <remarks>
/// Different implementations represent different language coding systems,
/// such as ISO 639 or BCP-47.
/// </remarks>
public abstract class LanguageCode(string value) : StringValueObject(value), ICode;