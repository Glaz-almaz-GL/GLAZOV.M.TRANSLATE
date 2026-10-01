using GLTranslate.Abstractions.Translation;
using System.Collections.Immutable;

namespace GLTranslate.Providers.GoogleCloud.Internal;

/// <summary>
/// One line of text as Vision read it, before it is translated.
/// </summary>
/// <param name="Text">
/// The line.
/// </param>
/// <param name="Bounds">
/// The box around the line.
/// </param>
/// <param name="Words">
/// The words of the line and where each of them sits.
/// </param>
internal readonly record struct RecognizedLine(string Text, TextBounds Bounds, ImmutableArray<RecognizedWord> Words);
