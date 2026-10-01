using GLAZOV.M.TRANSLATE.Abstractions.Providers;

namespace GLAZOV.M.TRANSLATE.Abstractions.Translation;

/// <summary>
/// Represents the capability of listening to a recording of speech and
/// translating what is said.
/// </summary>
/// <remarks>
/// The result carries what was heard and what it means in the target language,
/// and, when the provider offers it, the translation spoken aloud.
/// </remarks>
public interface IAudioTranslationProvider : IProvider, IProviderCapability<AudioTranslationRequest, AudioTranslationResult>
{
}
