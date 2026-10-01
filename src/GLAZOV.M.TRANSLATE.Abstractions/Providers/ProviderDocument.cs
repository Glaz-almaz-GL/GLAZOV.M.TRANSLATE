using System.Collections.Immutable;

namespace GLAZOV.M.TRANSLATE.Abstractions.Providers;

/// <summary>
/// Represents a document handed to a provider, or handed back by one.
/// </summary>
/// <remarks>
/// <para>
/// The kind of document is told by the extension of <see cref="FileName"/>:
/// <c>report.docx</c> is a Word document whatever else is known about it.
/// </para>
/// <para>
/// Instances of <see cref="ProviderDocument"/> are immutable and thread-safe.
/// </para>
/// </remarks>
public sealed class ProviderDocument
{
    /// <summary>
    /// Gets the bytes of the document.
    /// </summary>
    public ImmutableArray<byte> Content { get; }

    /// <summary>
    /// Gets the name of the file of the document, with its extension.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Gets the extension of <see cref="FileName"/> without the dot and in
    /// lowercase, such as <c>docx</c>, or an empty string when the name has none.
    /// </summary>
    public string Format { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderDocument"/> class.
    /// </summary>
    /// <param name="content">
    /// The bytes of the document.
    /// </param>
    /// <param name="fileName">
    /// The name of the file of the document, with its extension.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="fileName"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="content"/> is empty, or when
    /// <paramref name="fileName"/> is empty or consists only of white-space
    /// characters.
    /// </exception>
    public ProviderDocument(ReadOnlySpan<byte> content, string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        if (content.IsEmpty)
        {
            // Empty document
            throw new ArgumentException("A document cannot be empty.", nameof(content));
        }

        Content = [.. content];
        FileName = fileName;
        Format = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
    }
}
