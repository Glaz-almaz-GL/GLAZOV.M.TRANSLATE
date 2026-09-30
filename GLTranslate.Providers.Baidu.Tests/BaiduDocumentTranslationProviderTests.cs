using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;
using System.Net;
using System.Text;
using System.Text.Json;

namespace GLTranslate.Providers.Baidu.Tests;

/// <summary>
/// Verifies the document translation provider: the job it submits, how it
/// waits for it, and the document it gives back.
/// </summary>
public sealed class BaiduDocumentTranslationProviderTests
{
    private const string DownloadUrl = "https://bos.example.com/translated/report";

    private static readonly BaiduCredentials Credentials = new("2015063000000001", "1234567890");

    private static readonly BaiduDocumentTranslationOptions Quick = new(
        TimeSpan.FromMilliseconds(1),
        TimeSpan.FromSeconds(5));

    private static readonly byte[] Original = Encoding.UTF8.GetBytes("Hello");

    private static readonly byte[] Translated = Encoding.UTF8.GetBytes("Привет");

    /// <summary>
    /// Plays Baidu: accepts the job, reports it as running for the given
    /// number of questions and then as done, and serves the document.
    /// </summary>
    private sealed class Baidu(int runningAnswers = 1, string? finished = null, string? submitAnswer = null)
    {
        public const string DefaultFinished = """
            {"code":0,"msg":"success","data":{"requestId":123456,"status":1,"fileSrcUrl":"https://bos.example.com/translated/report","outPutDocType":"docx","from":"en","name":"report.docx"}}
            """;

        private int _questions;

        public List<HttpRequestMessage> Requests { get; } = [];

        public List<string> Bodies { get; } = [];

        public HttpResponseMessage Answer(HttpRequestMessage request)
        {
            Requests.Add(request);
            Bodies.Add(request.Content is null ? string.Empty : request.Content.ReadAsStringAsync().GetAwaiter().GetResult());

            string path = request.RequestUri!.AbsoluteUri;

            if (path == DownloadUrl)
            {
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Translated) };
            }

            string json = path.Contains("createjob/trans", StringComparison.Ordinal)
                ? submitAnswer ?? """{"code":0,"msg":"success","data":{"requestId":123456}}"""
                : ++_questions > runningAnswers
                    ? finished ?? DefaultFinished
                    : """{"code":0,"msg":"success","data":{"requestId":123456,"status":0}}""";

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        }
    }

    private static DocumentTranslationRequest Request(string fileName = "report.pdf", LanguageId? source = null)
    {
        return new DocumentTranslationRequest(
            new ProviderDocument(Original, fileName),
            new LanguageId("russian"),
            source);
    }

    [Fact]
    public void Name_IsBaidu()
    {
        using BaiduDocumentTranslationProvider provider = new(Credentials, new HttpClient());

        Assert.Equal("Baidu", provider.Name);
    }

    [Fact]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new BaiduDocumentTranslationProvider(null!));
        Assert.Throws<ArgumentNullException>(() => new BaiduDocumentTranslationProvider(null!, new HttpClient()));
        Assert.Throws<ArgumentNullException>(() => new BaiduDocumentTranslationProvider(Credentials, (HttpClient)null!));
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        using BaiduDocumentTranslationProvider provider = new(Credentials, new HttpClient(), Quick);

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsync_Job_SubmitsWaitsAndDownloads()
    {
        Baidu baidu = new(runningAnswers: 2);

        using StubHttpMessageHandler handler = new(baidu.Answer);
        using HttpClient httpClient = new(handler);
        using BaiduDocumentTranslationProvider provider = new(Credentials, httpClient, Quick);

        DocumentTranslationRequest request = Request();
        DocumentTranslationResult result = await provider.ExecuteAsync(request);

        // One submission, three questions (two "running" and one "done"), one download.
        Assert.Equal(5, baidu.Requests.Count);
        Assert.Contains("createjob/trans", baidu.Requests[0].RequestUri!.AbsoluteUri, StringComparison.Ordinal);
        Assert.All(baidu.Requests.Skip(1).Take(3), r => Assert.Contains("query/trans", r.RequestUri!.AbsoluteUri, StringComparison.Ordinal));
        Assert.Equal(DownloadUrl, baidu.Requests[4].RequestUri!.AbsoluteUri);

        Assert.Equal(Translated, result.TranslatedDocument.Content.ToArray());
        Assert.Equal(request.Id, result.RequestId);
        Assert.Equal("russian", result.TargetLanguageId.Value);
    }

    [Fact]
    public async Task ExecuteAsync_Submission_CarriesTheDocumentAndIsSignedOverTheBody()
    {
        Baidu baidu = new();

        using StubHttpMessageHandler handler = new(baidu.Answer);
        using HttpClient httpClient = new(handler);
        using BaiduDocumentTranslationProvider provider = new(Credentials, httpClient, Quick);

        await provider.ExecuteAsync(Request("report.docx", new LanguageId("english")));

        HttpRequestMessage submission = baidu.Requests[0];
        string body = baidu.Bodies[0];

        using JsonDocument json = JsonDocument.Parse(body);
        JsonElement root = json.RootElement;

        Assert.Equal("en", root.GetProperty("from").GetString());
        Assert.Equal("ru", root.GetProperty("to").GetString());
        Assert.Equal(Convert.ToBase64String(Original), root.GetProperty("input").GetProperty("content").GetString());
        Assert.Equal("docx", root.GetProperty("input").GetProperty("format").GetString());
        Assert.Equal("report.docx", root.GetProperty("input").GetProperty("filename").GetString());

        string timestamp = Assert.Single(submission.Headers.GetValues("X-Timestamp"));

        Assert.Equal("2015063000000001", Assert.Single(submission.Headers.GetValues("X-Appid")));
        Assert.Equal(
            Internal.BaiduSignature.ForHeaders("2015063000000001", timestamp, body, "1234567890"),
            Assert.Single(submission.Headers.GetValues("X-Sign")));
        Assert.DoesNotContain("1234567890", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_NoSource_AsksBaiduToDetectAndReportsWhatItFound()
    {
        Baidu baidu = new();

        using StubHttpMessageHandler handler = new(baidu.Answer);
        using HttpClient httpClient = new(handler);
        using BaiduDocumentTranslationProvider provider = new(Credentials, httpClient, Quick);

        DocumentTranslationResult result = await provider.ExecuteAsync(Request());

        using JsonDocument json = JsonDocument.Parse(baidu.Bodies[0]);

        Assert.Equal("auto", json.RootElement.GetProperty("from").GetString());
        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.True(result.WasSourceLanguageDetected);
    }

    [Fact]
    public async Task ExecuteAsync_DocumentComesBackInAnotherFormat_CarriesTheNewExtension()
    {
        Baidu baidu = new();

        using StubHttpMessageHandler handler = new(baidu.Answer);
        using HttpClient httpClient = new(handler);
        using BaiduDocumentTranslationProvider provider = new(Credentials, httpClient, Quick);

        // A PDF goes in; Baidu says the translation is a Word document.
        DocumentTranslationResult result = await provider.ExecuteAsync(Request("report.pdf"));

        Assert.Equal("report.docx", result.TranslatedDocument.FileName);
        Assert.Equal("docx", result.TranslatedDocument.Format);
    }

    [Fact]
    public async Task ExecuteAsync_FormatBaiduDoesNotTranslate_ThrowsBeforeAnyRequest()
    {
        Baidu baidu = new();

        using StubHttpMessageHandler handler = new(baidu.Answer);
        using HttpClient httpClient = new(handler);
        using BaiduDocumentTranslationProvider provider = new(Credentials, httpClient, Quick);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(Request("program.exe")));

        Assert.Contains("exe", exception.Message, StringComparison.Ordinal);
        Assert.Empty(baidu.Requests);
    }

    [Fact]
    public async Task ExecuteAsync_JobFails_ThrowsProviderExceptionWithTheReason()
    {
        Baidu baidu = new(
            runningAnswers: 0,
            finished: """{"code":0,"msg":"success","data":{"requestId":123456,"status":2,"reason":"file cannot be parsed"}}""");

        using StubHttpMessageHandler handler = new(baidu.Answer);
        using HttpClient httpClient = new(handler);
        using BaiduDocumentTranslationProvider provider = new(Credentials, httpClient, Quick);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(Request()));

        Assert.Contains("file cannot be parsed", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_JobNeverFinishes_ThrowsProviderExceptionWhenTheTimeIsUp()
    {
        Baidu baidu = new(runningAnswers: int.MaxValue);

        using StubHttpMessageHandler handler = new(baidu.Answer);
        using HttpClient httpClient = new(handler);
        using BaiduDocumentTranslationProvider provider = new(
            Credentials,
            httpClient,
            new BaiduDocumentTranslationOptions(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(30)));

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(Request()));

        Assert.Contains("did not finish", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_CancelledWhileWaiting_ThrowsOperationCanceledException()
    {
        Baidu baidu = new(runningAnswers: int.MaxValue);

        using StubHttpMessageHandler handler = new(baidu.Answer);
        using HttpClient httpClient = new(handler);
        using BaiduDocumentTranslationProvider provider = new(Credentials, httpClient, Quick);
        using CancellationTokenSource source = new(TimeSpan.FromMilliseconds(30));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.ExecuteAsync(Request(), source.Token));
    }

    [Fact]
    public async Task ExecuteAsync_SubmissionRefused_ThrowsProviderExceptionNamingTheCode()
    {
        Baidu baidu = new(submitAnswer: """{"code":10005,"msg":"Sign fail"}""");

        using StubHttpMessageHandler handler = new(baidu.Answer);
        using HttpClient httpClient = new(handler);
        using BaiduDocumentTranslationProvider provider = new(Credentials, httpClient, Quick);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(Request()));

        Assert.Contains("10005", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Sign fail", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_DoneWithoutAddress_ThrowsProviderException()
    {
        Baidu baidu = new(
            runningAnswers: 0,
            finished: """{"code":0,"msg":"success","data":{"requestId":123456,"status":1,"from":"en"}}""");

        using StubHttpMessageHandler handler = new(baidu.Answer);
        using HttpClient httpClient = new(handler);
        using BaiduDocumentTranslationProvider provider = new(Credentials, httpClient, Quick);

        await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(Request()));
    }
}
