using GLTranslate.Abstractions.Providers;

namespace GLTranslate.Abstractions.Translation;

/// <summary>
/// Represents the capability of translating markup while leaving its tags
/// where they are.
/// </summary>
/// <remarks>
/// This is a capability of its own rather than a flavour of
/// <see cref="ITextTranslationProvider"/>: a provider may translate text
/// without being able to keep markup intact, and a caller that hands markup
/// to a text translator gets its tags translated along with the words.
/// </remarks>
public interface IMarkupTranslationProvider : IProviderCapability<MarkupTranslationRequest, MarkupTranslationResult>
{
    /// <summary>
    /// Gets the name of the provider.
    /// </summary>
    string Name { get; }
}
