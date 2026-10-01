using GLAZOV.M.TRANSLATE.Abstractions.Common;
using GLAZOV.M.TRANSLATE.Abstractions.Interfaces;

namespace GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Regions;

/// <summary>
/// Represents the base class for all region code representations.
/// </summary>
/// <remarks>
/// Different implementations represent different region coding systems,
/// such as ISO 3166.
/// </remarks>
public abstract class RegionCode(string value) : StringValueObject(value), ICode;