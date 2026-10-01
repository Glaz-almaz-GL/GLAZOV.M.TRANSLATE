using System.Globalization;
using System.Text;

namespace GLAZOV.M.TRANSLATE.Providers.GoogleCloud.Tests;

/// <summary>
/// Builds the answers of Google Cloud Vision the tests feed the providers.
/// </summary>
internal static class VisionAnswer
{
    /// <summary>
    /// Builds a word: its symbols, where it sits, and the break after its last
    /// symbol.
    /// </summary>
    public static string Word(string text, int x, int y, int width, int height, string? breakType)
    {
        StringBuilder symbols = new();

        for (int index = 0; index < text.Length; index++)
        {
            if (index > 0)
            {
                symbols.Append(',');
            }

            symbols.Append("{\"text\":\"").Append(text[index]).Append('"');

            if (index == text.Length - 1 && breakType is not null)
            {
                symbols.Append(",\"property\":{\"detectedBreak\":{\"type\":\"").Append(breakType).Append("\"}}");
            }

            symbols.Append('}');
        }

        string Vertex(int vertexX, int vertexY)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{{\"x\":{vertexX},\"y\":{vertexY}}}");
        }

        return "{\"boundingBox\":{\"vertices\":["
            + Vertex(x, y) + "," + Vertex(x + width, y) + "," + Vertex(x + width, y + height) + "," + Vertex(x, y + height)
            + "]},\"symbols\":[" + symbols + "]}";
    }

    /// <summary>
    /// Builds the answer to one image whose text is a single paragraph of the
    /// given words.
    /// </summary>
    public static string Page(string? language, params string[] words)
    {
        string property = language is null
            ? string.Empty
            : "\"property\":{\"detectedLanguages\":[{\"languageCode\":\"" + language + "\"}]},";

        return "{\"responses\":[{\"fullTextAnnotation\":{\"pages\":[{" + property
            + "\"blocks\":[{\"paragraphs\":[{\"words\":[" + string.Join(',', words) + "]}]}]}]}}]}";
    }
}
