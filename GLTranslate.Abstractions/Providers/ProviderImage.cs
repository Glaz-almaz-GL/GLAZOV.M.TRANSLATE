using System.Collections.Immutable;

namespace GLTranslate.Abstractions.Providers;

/// <summary>
/// Represents an image handed to a provider.
/// </summary>
/// <remarks>
/// Instances of <see cref="ProviderImage"/> are immutable and thread-safe.
/// </remarks>
public sealed class ProviderImage
{
    /// <summary>
    /// Gets the bytes of the image.
    /// </summary>
    public ImmutableArray<byte> Content { get; }

    /// <summary>
    /// Gets the media type of the image, such as <c>image/jpeg</c>.
    /// </summary>
    public string MediaType { get; }

    /// <summary>
    /// Gets the file name to send the image under.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderImage"/> class.
    /// </summary>
    /// <param name="content">
    /// The bytes of the image.
    /// </param>
    /// <param name="mediaType">
    /// The media type of the image, such as <c>image/jpeg</c>.
    /// </param>
    /// <param name="fileName">
    /// The file name to send the image under, which some providers expect to
    /// see even though they read the bytes and not the name.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="mediaType"/> or <paramref name="fileName"/>
    /// is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="content"/> is empty, or when
    /// <paramref name="mediaType"/> or <paramref name="fileName"/> is empty or
    /// consists only of white-space characters.
    /// </exception>
    public ProviderImage(ReadOnlySpan<byte> content, string mediaType = "image/jpeg", string fileName = "image")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaType);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        if (content.IsEmpty)
        {
            // Empty image
            throw new ArgumentException("An image cannot be empty.", nameof(content));
        }

        Content = [.. content];
        MediaType = mediaType;
        FileName = fileName;
    }
}
