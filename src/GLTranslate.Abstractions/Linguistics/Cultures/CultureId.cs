using GLTranslate.Abstractions.Common;
using System.Diagnostics;

namespace GLTranslate.Abstractions.Linguistics.Cultures;

/// <summary>
/// Represents the unique identifier of a culture within GLTranslate.
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