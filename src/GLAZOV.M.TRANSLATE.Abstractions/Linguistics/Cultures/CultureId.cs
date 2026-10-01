using GLAZOV.M.TRANSLATE.Abstractions.Common;
using System.Diagnostics;

namespace GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Cultures;

/// <summary>
/// Represents the unique identifier of a culture within GLAZOV.M.TRANSLATE.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="CultureId"/> uniquely identifies a culture independently
/// from any external localization standard.
/// </para>
/// <para>
/// Instances of this class are immutable and thread-safe.
/// </para>
/// </remarks>
[DebuggerDisplay("{Value,nq}")]
public sealed class CultureId(string value) : StringValueObject(value);