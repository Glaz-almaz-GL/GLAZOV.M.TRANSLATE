using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Providers.Papago.Internal;
using System.Collections.Immutable;

namespace GLAZOV.M.TRANSLATE.Providers.Papago;

/// <summary>
/// Looks words up in the Naver dictionary behind Papago.
/// </summary>
/// <remarks>
/// <para>
/// The dictionary describes a word with its pronunciation, its meanings by part
/// of speech and sentences that show them in use. Its pairs of languages are
/// those of the Naver dictionaries: Korean with English, Japanese, Chinese and a
/// few more. A pair it does not have is refused by the service and reported as a
/// <see cref="ProviderException"/>; a word it does not know under the pair
/// gives an empty list.
/// </para>
/// <para>
/// Only Papago offers it, so it has no interface of its own in the abstractions:
/// the class is used directly. It is undocumented, unsupported by its owner, and
/// may stop working without notice; the address is a setting of
/// <see cref="PapagoOptions"/> for that reason.
/// </para>
/// <para>
/// Instances are immutable and thread-safe, provided the supplied
/// <see cref="HttpClient"/> is not mutated after construction.
/// </para>
/// </remarks>
public sealed class PapagoDictionaryProvider : IDisposable
{
    private readonly PapagoEngine _engine;

    /// <summary>
    /// Gets the name of the provider.
    /// </summary>
    public string Name => PapagoProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="PapagoDictionaryProvider"/>
    /// class with the default settings.
    /// </summary>
    public PapagoDictionaryProvider()
        : this(new PapagoOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PapagoDictionaryProvider"/>
    /// class with the given settings.
    /// </summary>
    /// <param name="options">The settings of the provider.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    public PapagoDictionaryProvider(PapagoOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _engine = new PapagoEngine(options);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PapagoDictionaryProvider"/>
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
    public PapagoDictionaryProvider(PapagoOptions options, HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new PapagoEngine(options, httpClient);
    }

    /// <summary>
    /// Looks a word up.
    /// </summary>
    /// <param name="word">The word to look up.</param>
    /// <param name="sourceLanguageId">The language of the word.</param>
    /// <param name="targetLanguageId">The language in which the meanings are written.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that completes with the entries in the order the service gives
    /// them; the list is empty when the dictionary has none.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when a parameter is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when a language is unknown or has no code Papago accepts, when
    /// Papago has no dictionary for the pair, or when the call fails.
    /// </exception>
    public Task<ImmutableArray<PapagoDictionaryEntry>> LookUpAsync(
        ProviderText word,
        LanguageId sourceLanguageId,
        LanguageId targetLanguageId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(word);
        ArgumentNullException.ThrowIfNull(sourceLanguageId);
        ArgumentNullException.ThrowIfNull(targetLanguageId);

        return _engine.LookUpAsync(
            word.Value,
            PapagoLanguageCodeResolver.Instance.ToProviderCode(sourceLanguageId),
            PapagoLanguageCodeResolver.Instance.ToProviderCode(targetLanguageId),
            cancellationToken);
    }

    /// <summary>
    /// Releases the resources the provider owns.
    /// </summary>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
