namespace GLAZOV.M.TRANSLATE.Providers.Microsoft.Tests;

/// <summary>
/// Marks a test that calls the real Microsoft endpoint over the network.
/// </summary>
/// <remarks>
/// Such a test depends on the network, on the endpoint being reachable and
/// on Microsoft not rate-limiting the caller, so it cannot decide whether the
/// code under test is correct. It is skipped unless the environment
/// variable <c>GLTRANSLATE_LIVE_TESTS</c> is set to <c>1</c>.
/// </remarks>
public sealed class LiveFactAttribute : FactAttribute
{
    private const string SwitchName = "GLTRANSLATE_LIVE_TESTS";

    /// <summary>
    /// Initializes a new instance of the <see cref="LiveFactAttribute"/> class.
    /// </summary>
    public LiveFactAttribute()
    {
        if (Environment.GetEnvironmentVariable(SwitchName) != "1")
        {
            Skip = $"Live network test. Set {SwitchName}=1 to run it.";
        }
    }
}
