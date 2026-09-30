namespace GLTranslate.Providers.GoogleCloud.Internal;

/// <summary>
/// Holds the facts shared by every Google Cloud capability of this assembly.
/// </summary>
internal static class GoogleCloudProvider
{
    /// <summary>
    /// The name reported by every Google Cloud provider and carried by every
    /// <see cref="Abstractions.Providers.ProviderException"/> it throws. It is
    /// not the name of the keyless Google provider, which is a different thing.
    /// </summary>
    public const string Name = "Google Cloud";
}
