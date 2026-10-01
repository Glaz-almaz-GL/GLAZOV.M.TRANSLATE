using GLTranslate.Abstractions.Common;

namespace GLTranslate.Abstractions.Linguistics.Scripts
{
    /// <summary>
    /// Represents the unique identifier of a script within GLTranslate.
    /// </summary>
    public sealed class ScriptId(string value) : StringValueObject(value);
}
