using GLAZOV.M.TRANSLATE.Abstractions.Common;

namespace GLAZOV.M.TRANSLATE.Abstractions.Providers;

/// <summary>
/// Represents a fragment of markup handed to or returned by a provider.
/// </summary>
/// <remarks>
/// <para>
/// Markup is not plain text: a provider that translates it leaves the tags
/// where they are and translates what stands between them. The distinction is
/// carried by the type so that markup is never sent where text is meant, and
/// the other way round.
/// </para>
/// <para>
/// Instances of <see cref="ProviderMarkup"/> are immutable and thread-safe.
/// </para>
/// </remarks>
/// <param name="value">
/// The markup.
/// </param>
/// <exception cref="ArgumentNullException">
/// Thrown when <paramref name="value"/> is <see langword="null"/>.
/// </exception>
/// <exception cref="ArgumentException">
/// Thrown when <paramref name="value"/> is empty or consists only of
/// white-space characters.
/// </exception>
public sealed class ProviderMarkup(string value) : StringValueObject(value);
