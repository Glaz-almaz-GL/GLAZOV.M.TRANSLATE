using System.Text.Json.Serialization;

namespace GLTranslate.Providers.Bing.Internal;

/// <summary>
/// Provides source-generated JSON (de)serialization metadata for the Bing
/// Translator models, avoiding reflection-based serialization at runtime.
/// </summary>
[JsonSerializable(typeof(BingTranslationResponse[]))]
internal sealed partial class BingTranslationJsonContext : JsonSerializerContext;
