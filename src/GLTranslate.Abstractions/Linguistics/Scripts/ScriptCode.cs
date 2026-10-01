using GLTranslate.Abstractions.Common;
using GLTranslate.Abstractions.Interfaces;

namespace GLTranslate.Abstractions.Linguistics.Scripts;

/// <summary>
/// Represents the base class for all script code representations.
/// </summary>
/// <remarks>
/// <para>
/// A script code identifies a writing system according to a specific
/// coding standard.
/// </para>
/// <para>
/// Different standards may represent the same script using different
/// textual values.
/// </para>
/// <para>
/// Implementations of this class are immutable and thread-safe.
/// </para>
/// </remarks>
public abstract class ScriptCode(string value) : StringValueObject(value), ICode;