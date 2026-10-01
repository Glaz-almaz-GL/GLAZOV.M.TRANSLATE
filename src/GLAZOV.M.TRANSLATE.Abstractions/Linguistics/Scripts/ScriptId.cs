using GLAZOV.M.TRANSLATE.Abstractions.Common;

namespace GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Scripts
{
    /// <summary>
    /// Represents the unique identifier of a script within GLAZOV.M.TRANSLATE.
    /// </summary>
    public sealed class ScriptId(string value) : StringValueObject(value);
}
