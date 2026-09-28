using System.Text.Json.Serialization;

namespace GLTranslate.Providers.Yandex.Internal;

/// <summary>
/// Provides source-generated JSON (de)serialization metadata for the Yandex
/// models, avoiding reflection-based serialization at runtime.
/// </summary>
[JsonSerializable(typeof(YandexTranslationResponse))]
[JsonSerializable(typeof(YandexDetectionResponse))]
[JsonSerializable(typeof(YandexOcrResponse))]
[JsonSerializable(typeof(string))]
internal sealed partial class YandexTranslationJsonContext : JsonSerializerContext;
