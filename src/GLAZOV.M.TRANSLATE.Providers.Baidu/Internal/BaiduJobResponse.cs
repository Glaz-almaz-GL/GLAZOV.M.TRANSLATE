using System.Text.Json.Serialization;

namespace GLAZOV.M.TRANSLATE.Providers.Baidu.Internal;

/// <summary>
/// The answer of the document endpoints of Baidu, both to the submission of a
/// job and to the question how it is going.
/// </summary>
internal sealed class BaiduJobResponse
{
    /// <summary>
    /// Gets or sets the code of the answer; zero means success.
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// Gets or sets the explanation of the answer.
    /// </summary>
    [JsonPropertyName("msg")]
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the data of the answer, which is there only on success.
    /// </summary>
    [JsonPropertyName("data")]
    public BaiduJobData? Data { get; set; }
}

/// <summary>
/// What Baidu says of a document translation job.
/// </summary>
internal sealed class BaiduJobData
{
    /// <summary>
    /// Gets or sets the identifier of the job.
    /// </summary>
    [JsonPropertyName("requestId")]
    public long RequestId { get; set; }

    /// <summary>
    /// Gets or sets how the job is going: 0 while it translates, 1 when it has
    /// succeeded, 2 when it has failed.
    /// </summary>
    [JsonPropertyName("status")]
    public int Status { get; set; }

    /// <summary>
    /// Gets or sets why the job failed.
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets where the translated document can be downloaded from.
    /// </summary>
    [JsonPropertyName("fileSrcUrl")]
    public string? FileUrl { get; set; }

    /// <summary>
    /// Gets or sets the format of the translated document, without a dot.
    /// </summary>
    [JsonPropertyName("outPutDocType")]
    public string? OutputFormat { get; set; }

    /// <summary>
    /// Gets or sets the code of the language the document was translated from.
    /// </summary>
    [JsonPropertyName("from")]
    public string? From { get; set; }
}
