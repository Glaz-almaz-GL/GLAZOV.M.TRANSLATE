using GLTranslate.Abstractions.Providers;

namespace GLTranslate.Abstractions.Transliteration;

/// <summary>
/// Represents a transliteration service integration.
/// </summary>
/// <remarks>
/// <para>
/// A provider is infrastructure: it depends on this abstraction, never the
/// other way around. Provider-specific identifiers and request formats
/// remain inside the provider implementation.
/// </para>
/// <para>
/// Implementations are expected to be immutable and thread-safe, since a
/// single provider instance may be shared across concurrent operations.
/// </para>
/// </remarks>
public interface ITransliterationProvider : IProvider, IProviderCapability<TransliterationRequest, TransliterationResult>
{
}
