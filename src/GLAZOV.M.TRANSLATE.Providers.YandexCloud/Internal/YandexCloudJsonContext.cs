using System.Text.Json.Serialization;

namespace GLAZOV.M.TRANSLATE.Providers.YandexCloud.Internal;

/// <summary>
/// Describes the requests and the answers of Yandex Cloud to the serializer, so
/// that reading and writing them needs no reflection.
/// </summary>
/// <remarks>
/// Yandex writes numbers as strings in some answers, so numbers are read from
/// either.
/// </remarks>
[JsonSourceGenerationOptions(NumberHandling = JsonNumberHandling.AllowReadingFromString)]
[JsonSerializable(typeof(YandexCloudTranslateRequest))]
[JsonSerializable(typeof(YandexCloudTranslateResponse))]
[JsonSerializable(typeof(YandexCloudRecognizeRequest))]
[JsonSerializable(typeof(YandexCloudRecognizeResponse))]
[JsonSerializable(typeof(YandexCloudErrorResponse))]
internal sealed partial class YandexCloudJsonContext : JsonSerializerContext;
