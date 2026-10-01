using GLTranslate.Abstractions.Translation;
using System.Collections.Immutable;
using System.Text;

namespace GLTranslate.Providers.GoogleCloud.Internal;

/// <summary>
/// Puts the words Google Cloud Vision read back into the lines they were
/// printed in.
/// </summary>
/// <remarks>
/// Vision reports text as blocks, paragraphs, words and symbols, and says
/// where a line ends only by the kind of break that follows a symbol. The
/// translation of an image works on lines, since a line is what has a place on
/// the picture that a translation can be drawn over.
/// </remarks>
internal static class GoogleCloudTextLines
{
    private const string Space = "SPACE";
    private const string SureSpace = "SURE_SPACE";
    private const string EndOfLine = "EOL_SURE_SPACE";
    private const string Hyphen = "HYPHEN";
    private const string LineBreak = "LINE_BREAK";

    /// <summary>
    /// Reads the lines of a page.
    /// </summary>
    /// <param name="page">
    /// The page Vision read.
    /// </param>
    /// <returns>
    /// The lines in reading order, each with its words and the box around it.
    /// Words that hold no text are left out.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="page"/> is <see langword="null"/>.
    /// </exception>
    public static ImmutableArray<RecognizedLine> Read(GoogleCloudPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        ImmutableArray<RecognizedLine>.Builder lines = ImmutableArray.CreateBuilder<RecognizedLine>();

        foreach (GoogleCloudParagraph paragraph in (page.Blocks ?? []).SelectMany(block => block.Paragraphs ?? []))
        {
            StringBuilder text = new();
            ImmutableArray<RecognizedWord>.Builder words = ImmutableArray.CreateBuilder<RecognizedWord>();
            TextBounds? bounds = null;

            foreach (GoogleCloudWord word in paragraph.Words ?? [])
            {
                string wordText = string.Concat((word.Symbols ?? []).Select(symbol => symbol.Text));

                if (wordText.Length == 0)
                {
                    continue;
                }

                TextBounds wordBounds = ToBounds(word.BoundingBox);

                text.Append(wordText);
                words.Add(new RecognizedWord(wordText, wordBounds));
                bounds = bounds is null ? wordBounds : Union(bounds.Value, wordBounds);

                string? breakType = (word.Symbols ?? [])
                    .LastOrDefault(symbol => symbol.Property?.DetectedBreak?.Type is not null)
                    ?.Property?.DetectedBreak?.Type;

                if (breakType is EndOfLine or Hyphen or LineBreak)
                {
                    lines.Add(new RecognizedLine(text.ToString(), bounds.Value, words.ToImmutable()));

                    text.Clear();
                    words.Clear();
                    bounds = null;
                }
                else if (breakType is Space or SureSpace)
                {
                    // Words of a line are told apart by a space, except in the
                    // scripts written without them, where Vision reports none.
                    text.Append(' ');
                }
            }

            if (words.Count > 0)
            {
                // A paragraph Vision does not end with a line break still ends.
                lines.Add(new RecognizedLine(text.ToString().TrimEnd(), bounds!.Value, words.ToImmutable()));
            }
        }

        return lines.ToImmutable();
    }

    private static TextBounds ToBounds(GoogleCloudBoundingBox? box)
    {
        return TextBounds.Enclosing((box?.Vertices ?? []).Select(vertex => (vertex.X, vertex.Y)));
    }

    private static TextBounds Union(TextBounds first, TextBounds second)
    {
        int left = Math.Min(first.X, second.X);
        int top = Math.Min(first.Y, second.Y);

        return new TextBounds(
            left,
            top,
            Math.Max(first.X + first.Width, second.X + second.Width) - left,
            Math.Max(first.Y + first.Height, second.Y + second.Height) - top);
    }
}
