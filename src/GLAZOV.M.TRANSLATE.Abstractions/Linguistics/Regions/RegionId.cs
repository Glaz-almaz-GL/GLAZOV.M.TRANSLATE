using GLAZOV.M.TRANSLATE.Abstractions.Common;

namespace GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Regions;

/// <summary>
/// Represents the unique identifier of a region within GLAZOV.M.TRANSLATE.
/// </summary>
public sealed class RegionId(string value) : StringValueObject(value);