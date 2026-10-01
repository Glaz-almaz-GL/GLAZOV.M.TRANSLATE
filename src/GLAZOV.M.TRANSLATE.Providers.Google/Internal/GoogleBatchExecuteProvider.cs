namespace GLAZOV.M.TRANSLATE.Providers.Google.Internal;

/// <summary>
/// Holds the name under which the <c>batchexecute</c> engine of Google Translate
/// reports itself.
/// </summary>
internal static class GoogleBatchExecuteProvider
{
    /// <summary>
    /// The name of the provider, distinct from <see cref="GoogleProvider.Name"/>
    /// so that a caller holding both can tell their answers and errors apart.
    /// </summary>
    public const string Name = "Google.BatchExecute";
}
