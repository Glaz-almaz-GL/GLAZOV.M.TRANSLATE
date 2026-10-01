using System.Text.Json.Serialization;

namespace GLTranslate.Providers.Google.Internal;

/// <summary>
/// Provides source-generated JSON (de)serialization metadata for the
/// Google Translate response models, avoiding reflection-based
/// serialization at runtime.
/// </summary>
[JsonSerializable(typeof(GoogleTranslationResponse))]
internal sealed partial class GoogleTranslationJsonContext : JsonSerializerContext;
