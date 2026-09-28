using GLTranslate.Abstractions.Common;

namespace GLTranslate.Abstractions.TextToSpeech;

/// <summary>
/// Represents the name of the voice a provider speaks with.
/// </summary>
/// <remarks>
/// <para>
/// A voice belongs to the provider that speaks it, not to the domain: the
/// same voice does not exist across providers, and a provider that offers no
/// choice of voice has none at all. The value is therefore passed to the
/// provider as it is given.
/// </para>
/// <para>
/// Instances of <see cref="VoiceName"/> are immutable and thread-safe.
/// </para>
/// </remarks>
/// <param name="value">
/// The name of the voice, as the provider names it.
/// </param>
/// <exception cref="ArgumentNullException">
/// Thrown when <paramref name="value"/> is <see langword="null"/>.
/// </exception>
/// <exception cref="ArgumentException">
/// Thrown when <paramref name="value"/> is empty or consists only of
/// white-space characters.
/// </exception>
public sealed class VoiceName(string value) : StringValueObject(value);
