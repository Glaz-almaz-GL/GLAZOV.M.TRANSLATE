namespace GLAZOV.M.TRANSLATE.Providers.Baidu;

/// <summary>
/// Represents how a Baidu document translation waits for its job.
/// </summary>
/// <remarks>
/// Baidu translates a document as a job that takes from seconds to minutes, so
/// the provider asks how it is going until it is told it is done. Asking is
/// free, but limited to a few times a second, and Baidu advises a question
/// every second for a small document and every ten for a large one. Instances
/// are immutable and thread-safe.
/// </remarks>
public sealed class BaiduDocumentTranslationOptions
{
    /// <summary>
    /// Gets the options with the default waiting: a question every two seconds,
    /// for at most ten minutes.
    /// </summary>
    public static BaiduDocumentTranslationOptions Default { get; } = new();

    /// <summary>
    /// Gets how long to wait between two questions about the job.
    /// </summary>
    public TimeSpan PollInterval { get; }

    /// <summary>
    /// Gets how long to wait for the job in all before giving up.
    /// </summary>
    public TimeSpan Timeout { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaiduDocumentTranslationOptions"/> class.
    /// </summary>
    /// <param name="pollInterval">
    /// How long to wait between two questions about the job, or
    /// <see langword="null"/> for two seconds.
    /// </param>
    /// <param name="timeout">
    /// How long to wait for the job in all, or <see langword="null"/> for ten
    /// minutes.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="pollInterval"/> or <paramref name="timeout"/>
    /// is not positive, or when <paramref name="timeout"/> is shorter than
    /// <paramref name="pollInterval"/>.
    /// </exception>
    public BaiduDocumentTranslationOptions(TimeSpan? pollInterval = null, TimeSpan? timeout = null)
    {
        PollInterval = pollInterval ?? TimeSpan.FromSeconds(2);
        Timeout = timeout ?? TimeSpan.FromMinutes(10);

        if (PollInterval <= TimeSpan.Zero)
        {
            // Not positive
            throw new ArgumentOutOfRangeException(nameof(pollInterval), PollInterval, "The time between two questions must be positive.");
        }

        if (Timeout < PollInterval)
        {
            // Timeout shorter than the interval
            throw new ArgumentOutOfRangeException(nameof(timeout), Timeout, "The time to wait for the job cannot be shorter than the time between two questions.");
        }
    }
}
