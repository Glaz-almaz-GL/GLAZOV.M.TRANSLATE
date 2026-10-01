using GLAZOV.M.TRANSLATE.Abstractions.Providers;

namespace GLAZOV.M.TRANSLATE.Abstractions.Translation;

/// <summary>
/// Represents a translation service integration (for example Google,
/// DeepL or Bing).
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
public interface ITextTranslationProvider : IProvider, IProviderCapability<TextTranslationRequest, TextTranslationResult>
{
}
