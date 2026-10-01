using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Domain.Linguistics.Languages.Generated;
using GLAZOV.M.TRANSLATE.Domain.Registries;

namespace GLAZOV.M.TRANSLATE.Domain.Linguistics.Languages;

/// <summary>
/// Represents an immutable registry of language entities.
/// </summary>
/// <remarks>
/// Provides read-only lookup of languages by identifier.
///
/// The registry is immutable and thread-safe.
/// </remarks>
public sealed partial class LanguageRegistry(IEnumerable<Language> languages) : ImmutableRegistry<Language, LanguageId>(languages)
{
    /// <summary>
    /// Gets the default registry, populated from ISO 639 language data.
    /// </summary>
    public static readonly LanguageRegistry Default = new(LanguageRegistryData.All);
}