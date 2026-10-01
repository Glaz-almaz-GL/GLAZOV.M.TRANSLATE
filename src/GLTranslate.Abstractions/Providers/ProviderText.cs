using GLTranslate.Abstractions.Common;

namespace GLTranslate.Abstractions.Providers;

/// <summary>
/// Represents a piece of natural-language text exchanged with a provider,
/// such as text to translate, translated output, or text to transliterate.
/// </summary>
/// <remarks>
/// Instances of this class are immutable and thread-safe.
/// </remarks>
public sealed class ProviderText(string value) : StringValueObject(value);
