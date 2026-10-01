using GLAZOV.M.TRANSLATE.Abstractions.TextToSpeech;
using System.Collections.Immutable;

namespace GLAZOV.M.TRANSLATE.Abstractions.Providers;

/// <summary>
/// Represents a recording handed to a provider.
/// </summary>
/// <remarks>
/// Instances of <see cref="ProviderAudio"/> are immutable and thread-safe.
/// </remarks>
public sealed class ProviderAudio
{
    /// <summary>
    /// Gets the bytes of the recording.
    /// </summary>
    public ImmutableArray<byte> Content { get; }

    /// <summary>
    /// Gets the content type of the recording, such as <c>audio/wav</c>.
    /// </summary>
    public AudioContentType ContentType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderAudio"/> class.
    /// </summary>
    /// <param name="content">
    /// The bytes of the recording.
    /// </param>
    /// <param name="contentType">
    /// The content type of the recording, such as <c>audio/wav</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="contentType"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="content"/> is empty.
    /// </exception>
    public ProviderAudio(ReadOnlySpan<byte> content, AudioContentType contentType)
    {
        ArgumentNullException.ThrowIfNull(contentType);

        if (content.IsEmpty)
        {
            // Empty recording
            throw new ArgumentException("A recording cannot be empty.", nameof(content));
        }

        Content = [.. content];
        ContentType = contentType;
    }
}
