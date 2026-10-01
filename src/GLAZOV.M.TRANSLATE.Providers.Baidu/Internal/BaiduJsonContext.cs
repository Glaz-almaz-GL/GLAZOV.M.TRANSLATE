using System.Text.Json.Serialization;

namespace GLAZOV.M.TRANSLATE.Providers.Baidu.Internal;

/// <summary>
/// Describes the answers of Baidu to the serializer, so that reading them
/// needs no reflection.
/// </summary>
/// <remarks>
/// Baidu writes its error codes as strings in some answers and as numbers in
/// others, so numbers are read from either.
/// </remarks>
[JsonSourceGenerationOptions(NumberHandling = JsonNumberHandling.AllowReadingFromString)]
[JsonSerializable(typeof(BaiduTextResponse))]
[JsonSerializable(typeof(BaiduPictureResponse))]
[JsonSerializable(typeof(BaiduJobResponse))]
[JsonSerializable(typeof(BaiduSpeechResponse))]
internal sealed partial class BaiduJsonContext : JsonSerializerContext;
