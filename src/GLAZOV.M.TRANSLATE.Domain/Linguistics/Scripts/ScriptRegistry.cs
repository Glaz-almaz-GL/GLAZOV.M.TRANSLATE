using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Scripts;
using GLAZOV.M.TRANSLATE.Domain.Linguistics.Scripts.Generated;
using GLAZOV.M.TRANSLATE.Domain.Registries;

namespace GLAZOV.M.TRANSLATE.Domain.Linguistics.Scripts;

/// <summary>
/// Represents an immutable registry of script entities.
/// </summary>
/// <remarks>
/// The registry provides read-only access to known scripts
/// using their identifiers.
///
/// The registry is immutable and thread-safe.
/// </remarks>
public sealed partial class ScriptRegistry(IEnumerable<Script> entities) : ImmutableRegistry<Script, ScriptId>(entities)
{
    /// <summary>
    /// Gets the default registry, populated from ISO 15924 script data.
    /// </summary>
    public static readonly ScriptRegistry Default = new(ScriptRegistryData.All);
}