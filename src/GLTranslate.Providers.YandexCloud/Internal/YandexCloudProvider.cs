namespace GLTranslate.Providers.YandexCloud.Internal;

/// <summary>
/// Holds the facts shared by every Yandex Cloud capability of this assembly.
/// </summary>
internal static class YandexCloudProvider
{
    /// <summary>
    /// The name reported by every Yandex Cloud provider and carried by every
    /// <see cref="Abstractions.Providers.ProviderException"/> it throws. It is
    /// not the name of the keyless Yandex provider, which is a different thing.
    /// </summary>
    public const string Name = "Yandex Cloud";
}
