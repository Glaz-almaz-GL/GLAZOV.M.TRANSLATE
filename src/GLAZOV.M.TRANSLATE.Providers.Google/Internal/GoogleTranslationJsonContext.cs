using System.Text.Json.Serialization;

namespace GLAZOV.M.TRANSLATE.Providers.Google.Internal;

/// <summary>
/// Provides source-generated JSON (de)serialization metadata for the
/// Google Translate response models, avoiding reflection-based
/// serialization at runtime.
/// </summary>
[JsonSerializable(typeof(GoogleTranslationResponse))]
internal sealed partial class GoogleTranslationJsonContext : JsonSerializerContext;
