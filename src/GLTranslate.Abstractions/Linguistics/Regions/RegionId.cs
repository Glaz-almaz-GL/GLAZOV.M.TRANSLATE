using GLTranslate.Abstractions.Common;

namespace GLTranslate.Abstractions.Linguistics.Regions;

/// <summary>
/// Represents the unique identifier of a region within GLTranslate.
/// </summary>
public sealed class RegionId(string value) : StringValueObject(value);