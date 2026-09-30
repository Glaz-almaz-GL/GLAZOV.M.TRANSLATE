using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;
using System.Net;
using System.Text;
using System.Web;

namespace GLTranslate.Providers.Baidu.Tests;

/// <summary>
/// Verifies the text translation provider: what it sends to Baidu and what it
/// makes of the answer.
/// </summary>
public sealed class BaiduTranslationProviderTests
{
    private static readonly BaiduCredentials Credentials = new("2015063000000001", "1234567890");

    private static StubHttpMessageHandler Answering(
        string json,
        HttpStatusCode status = HttpStatusCode.OK,
        Action<HttpRequestMessage, string>? observe = null)
    {
        return new StubHttpMessageHandler(request =>
        {
            observe?.Invoke(request, request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());

            return new HttpResponseMessage(status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        });
    }

    [Fact]
    public void Name_IsBaidu()
    {
        using BaiduTranslationProvider provider = new(Credentials, new HttpClient());

        Assert.Equal("Baidu", provider.Name);
    }

    [Fact]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new BaiduTranslationProvider(null!));
        Assert.Throws<ArgumentNullException>(() => new BaiduTranslationProvider(null!, new HttpClient()));
        Assert.Throws<ArgumentNullException>(() => new BaiduTranslationProvider(Credentials, null!));
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        using BaiduTranslationProvider provider = new(Credentials, new HttpClient());

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsync_ExplicitSource_ReturnsTranslationWithoutDetectionFlag()
    {
        using StubHttpMessageHandler handler = Answering(
            """{"from":"en","to":"zh","trans_result":[{"src":"apple","dst":"苹果"}]}""");
        using HttpClient httpClient = new(handler);
        using BaiduTranslationProvider provider = new(Credentials, httpClient);

        TextTranslationRequest request = new(
            new ProviderText("apple"),
            new LanguageId("chinese"),
            new LanguageId("english"));

        TextTranslationResult result = await provider.ExecuteAsync(request);

        Assert.Equal("苹果", result.TranslatedText.Value);
        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.Equal("chinese", result.TargetLanguageId.Value);
        Assert.False(result.WasSourceLanguageDetected);
        Assert.Equal(request.Id, result.RequestId);
    }

    [Fact]
    public async Task ExecuteAsync_NoSource_AsksBaiduToDetectItAndReportsWhatItFound()
    {
        string? body = null;

        using StubHttpMessageHandler handler = Answering(
            """{"from":"en","to":"ru","trans_result":[{"src":"apple","dst":"яблоко"}]}""",
            observe: (_, sent) => body = sent);
        using HttpClient httpClient = new(handler);
        using BaiduTranslationProvider provider = new(Credentials, httpClient);

        TextTranslationResult result = await provider.ExecuteAsync(
            new TextTranslationRequest(new ProviderText("apple"), new LanguageId("russian")));

        Assert.Contains("from=auto", body, StringComparison.Ordinal);
        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.True(result.WasSourceLanguageDetected);
    }

    [Fact]
    public async Task ExecuteAsync_SendsTheSignatureTheDocumentationDescribes()
    {
        Uri? uri = null;
        string? body = null;

        using StubHttpMessageHandler handler = Answering(
            """{"from":"en","to":"zh","trans_result":[{"src":"apple","dst":"苹果"}]}""",
            observe: (request, sent) =>
            {
                uri = request.RequestUri;
                body = sent;
            });
        using HttpClient httpClient = new(handler);
        using BaiduTranslationProvider provider = new(Credentials, httpClient);

        await provider.ExecuteAsync(new TextTranslationRequest(
            new ProviderText("apple"),
            new LanguageId("chinese"),
            new LanguageId("english")));

        Assert.Equal("https://fanyi-api.baidu.com/api/trans/vip/translate", uri!.GetLeftPart(UriPartial.Path));

        var fields = HttpUtility.ParseQueryString(body!);

        Assert.Equal("apple", fields["q"]);
        Assert.Equal("en", fields["from"]);
        Assert.Equal("zh", fields["to"]);
        Assert.Equal("2015063000000001", fields["appid"]);

        // The signature covers the app, the text, the salt and the secret key,
        // and the secret key itself is never sent.
        string expected = Internal.BaiduSignature.ForText("2015063000000001", "apple", fields["salt"]!, "1234567890");

        Assert.Equal(expected, fields["sign"]);
        Assert.DoesNotContain("1234567890", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_TextOfSeveralLines_ComesBackAsSeveralLines()
    {
        using StubHttpMessageHandler handler = Answering(
            """{"from":"en","to":"ru","trans_result":[{"src":"Good morning","dst":"Доброе утро"},{"src":"Good evening","dst":"Добрый вечер"}]}""");
        using HttpClient httpClient = new(handler);
        using BaiduTranslationProvider provider = new(Credentials, httpClient);

        TextTranslationResult result = await provider.ExecuteAsync(new TextTranslationRequest(
            new ProviderText("Good morning\nGood evening"),
            new LanguageId("russian"),
            new LanguageId("english")));

        Assert.Equal("Доброе утро\nДобрый вечер", result.TranslatedText.Value);
    }

    [Theory]
    [InlineData("""{"error_code":"54001","error_msg":"Invalid Sign"}""", "54001", "Invalid Sign")]
    [InlineData("""{"error_code":54003,"error_msg":"Access limit unauthorized user"}""", "54003", "Access limit")]
    public async Task ExecuteAsync_RefusedByBaidu_ThrowsProviderExceptionNamingTheCode(
        string json,
        string code,
        string message)
    {
        using StubHttpMessageHandler handler = Answering(json);
        using HttpClient httpClient = new(handler);
        using BaiduTranslationProvider provider = new(Credentials, httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new TextTranslationRequest(new ProviderText("apple"), new LanguageId("chinese"), new LanguageId("english"))));

        Assert.Equal("Baidu", exception.ProviderName);
        Assert.Contains(code, exception.Message, StringComparison.Ordinal);
        Assert.Contains(message, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_AnswerWithoutTranslation_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering("""{"from":"en","to":"zh","trans_result":[]}""");
        using HttpClient httpClient = new(handler);
        using BaiduTranslationProvider provider = new(Credentials, httpClient);

        await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new TextTranslationRequest(new ProviderText("apple"), new LanguageId("chinese"), new LanguageId("english"))));
    }

    [Fact]
    public async Task ExecuteAsync_HttpFailure_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering("{}", HttpStatusCode.InternalServerError);
        using HttpClient httpClient = new(handler);
        using BaiduTranslationProvider provider = new(Credentials, httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new TextTranslationRequest(new ProviderText("apple"), new LanguageId("chinese"), new LanguageId("english"))));

        Assert.IsType<HttpRequestException>(exception.InnerException);
    }

    [Fact]
    public async Task ExecuteAsync_AnswerThatIsNotJson_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering("<html>oops</html>");
        using HttpClient httpClient = new(handler);
        using BaiduTranslationProvider provider = new(Credentials, httpClient);

        await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new TextTranslationRequest(new ProviderText("apple"), new LanguageId("chinese"), new LanguageId("english"))));
    }

    [Fact]
    public async Task ExecuteAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        using HttpClient httpClient = new(Answering("{}"));
        BaiduTranslationProvider provider = new(Credentials, httpClient);

        provider.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => provider.ExecuteAsync(
            new TextTranslationRequest(new ProviderText("apple"), new LanguageId("chinese"), new LanguageId("english"))));
    }
}
