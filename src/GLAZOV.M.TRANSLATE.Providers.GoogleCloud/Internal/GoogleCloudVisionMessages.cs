using System.Text.Json.Serialization;

namespace GLAZOV.M.TRANSLATE.Providers.GoogleCloud.Internal;

/// <summary>
/// The body Google Cloud Vision takes.
/// </summary>
internal sealed class GoogleCloudAnnotateRequest
{
    /// <summary>
    /// Gets or sets the requests; the provider sends one.
    /// </summary>
    [JsonPropertyName("requests")]
    public IReadOnlyList<GoogleCloudAnnotateImageRequest> Requests { get; set; } = [];
}

/// <summary>
/// One image Google Cloud Vision is asked about.
/// </summary>
internal sealed class GoogleCloudAnnotateImageRequest
{
    /// <summary>
    /// Gets or sets the image.
    /// </summary>
    [JsonPropertyName("image")]
    public GoogleCloudVisionImage Image { get; set; } = new();

    /// <summary>
    /// Gets or sets what to find in it.
    /// </summary>
    [JsonPropertyName("features")]
    public IReadOnlyList<GoogleCloudVisionFeature> Features { get; set; } = [];

    /// <summary>
    /// Gets or sets hints, which are the languages the text is likely written in.
    /// </summary>
    [JsonPropertyName("imageContext")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GoogleCloudVisionContext? ImageContext { get; set; }
}

/// <summary>
/// An image sent to Google Cloud Vision.
/// </summary>
internal sealed class GoogleCloudVisionImage
{
    /// <summary>
    /// Gets or sets the bytes of the image, as base64.
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// A kind of thing Google Cloud Vision is asked to find.
/// </summary>
internal sealed class GoogleCloudVisionFeature
{
    /// <summary>
    /// Gets or sets the kind, such as <c>DOCUMENT_TEXT_DETECTION</c>.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// What is known about an image beyond its pixels.
/// </summary>
internal sealed class GoogleCloudVisionContext
{
    /// <summary>
    /// Gets or sets the codes of the languages the text is likely written in.
    /// </summary>
    [JsonPropertyName("languageHints")]
    public IReadOnlyList<string> LanguageHints { get; set; } = [];
}

/// <summary>
/// The answer of Google Cloud Vision.
/// </summary>
internal sealed class GoogleCloudAnnotateResponse
{
    /// <summary>
    /// Gets or sets one answer for each image sent.
    /// </summary>
    [JsonPropertyName("responses")]
    public IReadOnlyList<GoogleCloudAnnotateImageResponse>? Responses { get; set; }
}

/// <summary>
/// The answer about one image.
/// </summary>
internal sealed class GoogleCloudAnnotateImageResponse
{
    /// <summary>
    /// Gets or sets the text found on the image, which is missing when there
    /// is none.
    /// </summary>
    [JsonPropertyName("fullTextAnnotation")]
    public GoogleCloudTextAnnotation? FullText { get; set; }

    /// <summary>
    /// Gets or sets the failure of this image, if it failed.
    /// </summary>
    [JsonPropertyName("error")]
    public GoogleCloudError? Error { get; set; }
}

/// <summary>
/// The text found on an image, page by page.
/// </summary>
internal sealed class GoogleCloudTextAnnotation
{
    /// <summary>
    /// Gets or sets the pages; an image has one.
    /// </summary>
    [JsonPropertyName("pages")]
    public IReadOnlyList<GoogleCloudPage>? Pages { get; set; }
}

/// <summary>
/// A page of text.
/// </summary>
internal sealed class GoogleCloudPage
{
    /// <summary>
    /// Gets or sets what is known of the page as a whole.
    /// </summary>
    [JsonPropertyName("property")]
    public GoogleCloudTextProperty? Property { get; set; }

    /// <summary>
    /// Gets or sets the blocks of text.
    /// </summary>
    [JsonPropertyName("blocks")]
    public IReadOnlyList<GoogleCloudBlock>? Blocks { get; set; }
}

/// <summary>
/// What is known of a piece of text beyond its letters.
/// </summary>
internal sealed class GoogleCloudTextProperty
{
    /// <summary>
    /// Gets or sets the languages detected, most likely first.
    /// </summary>
    [JsonPropertyName("detectedLanguages")]
    public IReadOnlyList<GoogleCloudDetectedLanguage>? DetectedLanguages { get; set; }

    /// <summary>
    /// Gets or sets what follows a symbol: a space or the end of a line.
    /// </summary>
    [JsonPropertyName("detectedBreak")]
    public GoogleCloudDetectedBreak? DetectedBreak { get; set; }
}

/// <summary>
/// A language Google Cloud Vision thinks the text is in.
/// </summary>
internal sealed class GoogleCloudDetectedLanguage
{
    /// <summary>
    /// Gets or sets the code of the language.
    /// </summary>
    [JsonPropertyName("languageCode")]
    public string? LanguageCode { get; set; }
}

/// <summary>
/// What follows a symbol.
/// </summary>
internal sealed class GoogleCloudDetectedBreak
{
    /// <summary>
    /// Gets or sets the kind of break: <c>SPACE</c>, <c>SURE_SPACE</c>,
    /// <c>EOL_SURE_SPACE</c>, <c>HYPHEN</c> or <c>LINE_BREAK</c>.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

/// <summary>
/// A block of text: a column, a table or a picture caption.
/// </summary>
internal sealed class GoogleCloudBlock
{
    /// <summary>
    /// Gets or sets the paragraphs of the block.
    /// </summary>
    [JsonPropertyName("paragraphs")]
    public IReadOnlyList<GoogleCloudParagraph>? Paragraphs { get; set; }
}

/// <summary>
/// A paragraph of text.
/// </summary>
internal sealed class GoogleCloudParagraph
{
    /// <summary>
    /// Gets or sets the words of the paragraph, in reading order.
    /// </summary>
    [JsonPropertyName("words")]
    public IReadOnlyList<GoogleCloudWord>? Words { get; set; }
}

/// <summary>
/// A word: symbols and where they sit.
/// </summary>
internal sealed class GoogleCloudWord
{
    /// <summary>
    /// Gets or sets where the word sits.
    /// </summary>
    [JsonPropertyName("boundingBox")]
    public GoogleCloudBoundingBox? BoundingBox { get; set; }

    /// <summary>
    /// Gets or sets the symbols of the word.
    /// </summary>
    [JsonPropertyName("symbols")]
    public IReadOnlyList<GoogleCloudSymbol>? Symbols { get; set; }
}

/// <summary>
/// A letter, digit or sign.
/// </summary>
internal sealed class GoogleCloudSymbol
{
    /// <summary>
    /// Gets or sets the symbol as text.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets what follows the symbol.
    /// </summary>
    [JsonPropertyName("property")]
    public GoogleCloudTextProperty? Property { get; set; }
}

/// <summary>
/// A quadrilateral around a piece of text, by its four corners.
/// </summary>
internal sealed class GoogleCloudBoundingBox
{
    /// <summary>
    /// Gets or sets the corners.
    /// </summary>
    [JsonPropertyName("vertices")]
    public IReadOnlyList<GoogleCloudVertex>? Vertices { get; set; }
}

/// <summary>
/// A corner of a <see cref="GoogleCloudBoundingBox"/>, in pixels. Google leaves
/// out a coordinate that is zero, which reads back as zero.
/// </summary>
internal sealed class GoogleCloudVertex
{
    /// <summary>
    /// Gets or sets the distance from the left edge.
    /// </summary>
    [JsonPropertyName("x")]
    public int X { get; set; }

    /// <summary>
    /// Gets or sets the distance from the top edge.
    /// </summary>
    [JsonPropertyName("y")]
    public int Y { get; set; }
}
