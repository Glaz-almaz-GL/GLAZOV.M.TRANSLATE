using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Abstractions.Translation;
using System.Net;
using System.Text;
using System.Web;

namespace GLAZOV.M.TRANSLATE.Providers.Baidu.Tests;

/// <summary>
/// Verifies the image translation provider: what it sends to Baidu, and how
/// the segments Baidu read come back as lines with their places.
/// </summary>
public sealed class BaiduImageTranslationProviderTests
{
    private const string Answer = """
        {"error_code":"0","error_msg":"success","data":{"from":"en","to":"ru","content":[
        {"src":"Good morning ","dst":"Доброе утро","rect":"10 59 374 20","lineCount":1},
        {"src":"Good evening","dst":"Добрый вечер","rect":"10 90 300 20","lineCount":1}],
        "sumSrc":"Good morning Good evening","sumDst":"Доброе утро Добрый вечер"}}
        """;

    private static readonly BaiduCredentials Credentials = new("2015063000000001", "1234567890");

    private static readonly byte[] Image = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static StubHttpMessageHandler Answering(string json, Action<HttpRequestMessage>? observe = null)
    {
        return new StubHttpMessageHandler(request =>
        {
            observe?.Invoke(request);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        });
    }

    [Fact]
    public void Name_IsBaidu()
    {
        using BaiduImageTranslationProvider provider = new(Credentials, new HttpClient());

        Assert.Equal("Baidu", provider.Name);
    }

    [Fact]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new BaiduImageTranslationProvider(null!));
        Assert.Throws<ArgumentNullException>(() => new BaiduImageTranslationProvider(Credentials, null!));
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        using BaiduImageTranslationProvider provider = new(Credentials, new HttpClient());

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsync_Answer_ReturnsEveryLineWithItsPlace()
    {
        using StubHttpMessageHandler handler = Answering(Answer);
        using HttpClient httpClient = new(handler);
        using BaiduImageTranslationProvider provider = new(Credentials, httpClient);

        ImageTranslationRequest request = new(new ProviderImage(Image, "image/png"), new LanguageId("russian"));
        ImageTranslationResult result = await provider.ExecuteAsync(request);

        Assert.Equal(2, result.Lines.Length);

        TranslatedLine first = result.Lines[0];

        Assert.Equal("Good morning", first.RecognizedText);
        Assert.Equal("Доброе утро", first.TranslatedText);
        Assert.Equal(new TextBounds(10, 59, 374, 20), first.Bounds);
        Assert.Empty(first.Words);

        Assert.Equal("Добрый вечер", result.Lines[1].TranslatedText);
        Assert.Equal(new TextBounds(10, 90, 300, 20), result.Lines[1].Bounds);

        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.Equal("russian", result.TargetLanguageId.Value);
        Assert.True(result.WasSourceLanguageDetected);
        Assert.Equal(request.Id, result.RequestId);
    }

    [Fact]
    public async Task ExecuteAsync_SendsTheImageAsAFileAndSignsItsHash()
    {
        Uri? uri = null;
        string? contentType = null;
        byte[]? sentImage = null;
        string? fileName = null;

        using StubHttpMessageHandler handler = Answering(Answer, request =>
        {
            uri = request.RequestUri;

            MultipartFormDataContent form = Assert.IsType<MultipartFormDataContent>(request.Content);
            HttpContent part = Assert.Single(form);

            contentType = part.Headers.ContentType!.MediaType;
            fileName = part.Headers.ContentDisposition!.FileName;
            sentImage = part.ReadAsByteArrayAsync().GetAwaiter().GetResult();
        });
        using HttpClient httpClient = new(handler);
        using BaiduImageTranslationProvider provider = new(Credentials, httpClient);

        await provider.ExecuteAsync(new ImageTranslationRequest(
            new ProviderImage(Image, "image/png"),
            new LanguageId("russian"),
            new LanguageId("english")));

        Assert.Equal("https://fanyi-api.baidu.com/api/trans/sdk/picture", uri!.GetLeftPart(UriPartial.Path));
        Assert.Equal("image/png", contentType);
        Assert.Equal("image.png", fileName);
        Assert.Equal(Image, sentImage);

        var query = HttpUtility.ParseQueryString(uri.Query);

        Assert.Equal("en", query["from"]);
        Assert.Equal("ru", query["to"]);
        Assert.Equal("2015063000000001", query["appid"]);
        Assert.Equal("APICUID", query["cuid"]);
        Assert.Equal("mac", query["mac"]);
        Assert.Equal("3", query["version"]);
        Assert.Equal(
            Internal.BaiduSignature.ForImage("2015063000000001", Image, query["salt"]!, "APICUID", "mac", "1234567890"),
            query["sign"]);
        Assert.DoesNotContain("1234567890", uri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_NoSource_AsksBaiduToDetectIt()
    {
        Uri? uri = null;

        using StubHttpMessageHandler handler = Answering(Answer, request => uri = request.RequestUri);
        using HttpClient httpClient = new(handler);
        using BaiduImageTranslationProvider provider = new(Credentials, httpClient);

        await provider.ExecuteAsync(new ImageTranslationRequest(new ProviderImage(Image, "image/png"), new LanguageId("russian")));

        Assert.Equal("auto", HttpUtility.ParseQueryString(uri!.Query)["from"]);
    }

    [Fact]
    public async Task ExecuteAsync_JpegImage_IsSentAsJpeg()
    {
        string? fileName = null;

        using StubHttpMessageHandler handler = Answering(Answer, request =>
        {
            fileName = Assert.Single((MultipartFormDataContent)request.Content!).Headers.ContentDisposition!.FileName;
        });
        using HttpClient httpClient = new(handler);
        using BaiduImageTranslationProvider provider = new(Credentials, httpClient);

        await provider.ExecuteAsync(new ImageTranslationRequest(new ProviderImage(Image, "image/jpeg"), new LanguageId("russian")));

        Assert.Equal("image.jpg", fileName);
    }

    [Fact]
    public async Task ExecuteAsync_ImageBaiduDoesNotRead_ThrowsProviderExceptionBeforeAnyRequest()
    {
        bool sent = false;

        using StubHttpMessageHandler handler = Answering(Answer, _ => sent = true);
        using HttpClient httpClient = new(handler);
        using BaiduImageTranslationProvider provider = new(Credentials, httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new ImageTranslationRequest(new ProviderImage(Image, "image/webp"), new LanguageId("russian"))));

        Assert.Contains("image/webp", exception.Message, StringComparison.Ordinal);
        Assert.False(sent);
    }

    [Fact]
    public async Task ExecuteAsync_NothingOnTheImage_ReturnsNoLines()
    {
        using StubHttpMessageHandler handler = Answering(
            """{"error_code":"0","error_msg":"success","data":{"from":"en","to":"ru","content":[]}}""");
        using HttpClient httpClient = new(handler);
        using BaiduImageTranslationProvider provider = new(Credentials, httpClient);

        ImageTranslationResult result = await provider.ExecuteAsync(
            new ImageTranslationRequest(new ProviderImage(Image, "image/png"), new LanguageId("russian")));

        Assert.Empty(result.Lines);
        Assert.Equal("english", result.SourceLanguageId.Value);
    }

    [Fact]
    public async Task ExecuteAsync_SegmentWithoutText_IsLeftOut()
    {
        using StubHttpMessageHandler handler = Answering(
            """
            {"error_code":"0","data":{"from":"en","content":[
            {"src":"  ","dst":"","rect":"0 0 1 1"},
            {"src":"Hello","dst":"Привет","rect":"1 2 3 4"}]}}
            """);
        using HttpClient httpClient = new(handler);
        using BaiduImageTranslationProvider provider = new(Credentials, httpClient);

        ImageTranslationResult result = await provider.ExecuteAsync(
            new ImageTranslationRequest(new ProviderImage(Image, "image/png"), new LanguageId("russian")));

        TranslatedLine line = Assert.Single(result.Lines);

        Assert.Equal("Привет", line.TranslatedText);
    }

    [Fact]
    public async Task ExecuteAsync_PositionBaiduSpoiled_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering(
            """{"error_code":"0","data":{"from":"en","content":[{"src":"Hello","dst":"Привет","rect":"left top"}]}}""");
        using HttpClient httpClient = new(handler);
        using BaiduImageTranslationProvider provider = new(Credentials, httpClient);

        await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new ImageTranslationRequest(new ProviderImage(Image, "image/png"), new LanguageId("russian"))));
    }

    [Theory]
    [InlineData("""{"error_code":"69004","error_msg":"empty content"}""", "69004")]
    [InlineData("""{"error_code":"58001","error_msg":"unsupported direction"}""", "58001")]
    public async Task ExecuteAsync_RefusedByBaidu_ThrowsProviderExceptionNamingTheCode(string json, string code)
    {
        using StubHttpMessageHandler handler = Answering(json);
        using HttpClient httpClient = new(handler);
        using BaiduImageTranslationProvider provider = new(Credentials, httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new ImageTranslationRequest(new ProviderImage(Image, "image/png"), new LanguageId("russian"))));

        Assert.Equal("Baidu", exception.ProviderName);
        Assert.Contains(code, exception.Message, StringComparison.Ordinal);
    }
}
