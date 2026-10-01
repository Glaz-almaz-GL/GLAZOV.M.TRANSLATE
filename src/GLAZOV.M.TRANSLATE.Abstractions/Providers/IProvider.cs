namespace GLAZOV.M.TRANSLATE.Abstractions.Providers;

/// <summary>
/// Represents a translation service integration, whatever it is able to do:
/// the part every capability of a provider has in common.
/// </summary>
/// <remarks>
/// <para>
/// A provider is infrastructure: it depends on this abstraction, never the
/// other way around. Provider-specific identifiers and request formats remain
/// inside the provider implementation.
/// </para>
/// <para>
/// Implementations are expected to be immutable and thread-safe, since a
/// single provider instance may be shared across concurrent operations.
/// </para>
/// </remarks>
public interface IProvider
{
    /// <summary>
    /// Gets the display name of the provider (for example <c>"Google"</c>).
    /// </summary>
    string Name { get; }
}
