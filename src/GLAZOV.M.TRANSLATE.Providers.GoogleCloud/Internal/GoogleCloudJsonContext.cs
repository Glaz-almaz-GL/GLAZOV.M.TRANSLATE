using System.Text.Json.Serialization;

namespace GLAZOV.M.TRANSLATE.Providers.GoogleCloud.Internal;

/// <summary>
/// Describes the requests and the answers of Google Cloud to the serializer,
/// so that reading and writing them needs no reflection.
/// </summary>
[JsonSerializable(typeof(GoogleCloudTranslateRequest))]
[JsonSerializable(typeof(GoogleCloudTranslateResponse))]
[JsonSerializable(typeof(GoogleCloudSynthesizeRequest))]
[JsonSerializable(typeof(GoogleCloudSynthesizeResponse))]
[JsonSerializable(typeof(GoogleCloudVoicesResponse))]
[JsonSerializable(typeof(GoogleCloudAnnotateRequest))]
[JsonSerializable(typeof(GoogleCloudAnnotateResponse))]
[JsonSerializable(typeof(GoogleCloudErrorResponse))]
internal sealed partial class GoogleCloudJsonContext : JsonSerializerContext;
