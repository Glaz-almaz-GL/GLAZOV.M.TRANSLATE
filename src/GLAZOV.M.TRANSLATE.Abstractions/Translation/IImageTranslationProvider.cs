using GLAZOV.M.TRANSLATE.Abstractions.Providers;

namespace GLAZOV.M.TRANSLATE.Abstractions.Translation;

/// <summary>
/// Represents the capability of reading the text on an image and translating
/// it.
/// </summary>
/// <remarks>
/// The result keeps every line where it was found, so that a caller can draw
/// the translation over the image it came from.
/// </remarks>
public interface IImageTranslationProvider : IProvider, IProviderCapability<ImageTranslationRequest, ImageTranslationResult>
{
}
