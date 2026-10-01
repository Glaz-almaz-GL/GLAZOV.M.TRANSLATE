using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Providers.Google.Internal;
using System.Collections.Immutable;

namespace GLAZOV.M.TRANSLATE.Providers.Google;

/// <summary>
/// Suggests the phrases Google Translate offers to complete what is being typed,
/// each with its translation.
/// </summary>
/// <remarks>
/// <para>
/// It is the call behind the suggestions the Google Translate page shows under
/// its input box. Only Google offers it, so it has no interface of its own in
/// the abstractions: the class is used directly.
/// </para>
/// <para>
/// The service is undocumented, unsupported by Google, and may change or stop
/// working without notice; the name of the call is a setting of
/// <see cref="GoogleBatchExecuteOptions"/> for that reason.
/// </para>
/// <para>
/// Instances are immutable and thread-safe, provided the supplied
/// <see cref="HttpClient"/> is not mutated after construction.
/// </para>
/// </remarks>
public sealed class GoogleSuggestionProvider : IDisposable
{
    private readonly GoogleBatchExecuteEngine _engine;

    /// <summary>
    /// Gets the name of the provider.
    /// </summary>
    public string Name => GoogleBatchExecuteProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleSuggestionProvider"/>
    /// class with the default settings.
    /// </summary>
    public GoogleSuggestionProvider()
        : this(new GoogleBatchExecuteOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleSuggestionProvider"/>
    /// class with the given settings.
    /// </summary>
    /// <param name="options">The settings of the provider.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    public GoogleSuggestionProvider(GoogleBatchExecuteOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _engine = new GoogleBatchExecuteEngine(options);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleSuggestionProvider"/>
    /// class that sends its requests through the given client.
    /// </summary>
    /// <param name="options">The settings of the provider.</param>
    /// <param name="httpClient">
    /// The HTTP client used to send requests. The provider does not own its
    /// lifetime.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> or <paramref name="httpClient"/>
    /// is <see langword="null"/>.
    /// </exception>
    public GoogleSuggestionProvider(GoogleBatchExecuteOptions options, HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new GoogleBatchExecuteEngine(options, httpClient);
    }

    /// <summary>
    /// Asks for the phrases that begin like the given text.
    /// </summary>
    /// <param name="text">The beginning of a phrase, in the source language.</param>
    /// <param name="sourceLanguageId">
    /// The language of the text. It cannot be left to detection: the service
    /// answers nothing to a text whose language it must guess.
    /// </param>
    /// <param name="targetLanguageId">The language to translate the phrases into.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that completes with the suggestions in the order Google gives them;
    /// the list is empty when Google has none.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when a parameter is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when a language is unknown or has no code Google accepts, or when
    /// the call fails.
    /// </exception>
    public async Task<ImmutableArray<GoogleSuggestion>> SuggestAsync(
        ProviderText text,
        LanguageId sourceLanguageId,
        LanguageId targetLanguageId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(sourceLanguageId);
        ArgumentNullException.ThrowIfNull(targetLanguageId);

        ImmutableArray<(string Phrase, string Translation)> pairs = await _engine
            .SuggestAsync(
                text.Value,
                GoogleLanguageCodeResolver.ToGoogleCode(sourceLanguageId),
                GoogleLanguageCodeResolver.ToGoogleCode(targetLanguageId),
                cancellationToken)
            .ConfigureAwait(false);

        return [.. pairs.Select(pair => new GoogleSuggestion(pair.Phrase, pair.Translation))];
    }

    /// <summary>
    /// Releases the resources the provider owns.
    /// </summary>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
