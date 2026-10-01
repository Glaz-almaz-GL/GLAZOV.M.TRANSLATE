using System.Text.Json.Serialization;

namespace GLAZOV.M.TRANSLATE.Providers.Microsoft.Internal;

/// <summary>
/// Provides source-generated JSON (de)serialization metadata for the
/// Microsoft Translator models, avoiding reflection-based serialization
/// at runtime.
/// </summary>
[JsonSerializable(typeof(MicrosoftTranslationRequest[]))]
[JsonSerializable(typeof(MicrosoftTranslationResponse[]))]
[JsonSerializable(typeof(MicrosoftTransliteration[]))]
[JsonSerializable(typeof(MicrosoftSpeechToken))]
internal sealed partial class MicrosoftTranslationJsonContext : JsonSerializerContext;
