using GLTranslate.Abstractions.Common;
using GLTranslate.Abstractions.Interfaces;

namespace GLTranslate.Abstractions.Linguistics.Cultures;

/// <summary>
/// Represents the base class for all culture code representations.
/// </summary>
/// <remarks>
/// <para>
/// A culture code represents a culture using a particular localization
/// standard or provider-specific format.
/// </para>
/// <para>
/// Implementations of this class are immutable and thread-safe.
/// </para>
/// </remarks>
public abstract class CultureCode(string value) : StringValueObject(value), ICode;