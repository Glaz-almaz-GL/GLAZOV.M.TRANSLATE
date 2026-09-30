using GLTranslate.Abstractions.Providers;

namespace GLTranslate.Abstractions.Translation;

/// <summary>
/// Represents the capability of translating a whole document and giving back
/// a document of the same kind.
/// </summary>
/// <remarks>
/// Providers translate documents as jobs that take a while; an implementation
/// waits for the job to finish, so the returned task completes only with the
/// translated document.
/// </remarks>
public interface IDocumentTranslationProvider : IProviderCapability<DocumentTranslationRequest, DocumentTranslationResult>
{
    /// <summary>
    /// Gets the name of the provider.
    /// </summary>
    string Name { get; }
}
