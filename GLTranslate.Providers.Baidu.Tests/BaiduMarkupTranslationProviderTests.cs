using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;
using System.Net;
using System.Text;
using System.Web;

namespace GLTranslate.Providers.Baidu.Tests;

/// <summary>
/// Verifies the markup translation provider: that it asks the endpoint that
/// keeps tags, and gives back the markup it answers with.
/// </summary>
public sealed class BaiduMarkupTranslationProviderTests
{
    private static readonly BaiduCredentials Credentials = new("2015063000000001", "1234567890");

    private static StubHttpMessageHandler Answering(string json, Action<HttpRequestMessage, string>? observe = null)
    {
        return new StubHttpMessageHandler(request =>
        {
            observe?.Invoke(request, request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        });
    }

    [Fact]
    public void Name_IsBaidu()
    {
        using BaiduMarkupTranslationProvider provider = new(Credentials, new HttpClient());

        Assert.Equal("Baidu", provider.Name);
    }

    [Fact]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new BaiduMarkupTranslationProvider(null!));
        Assert.Throws<ArgumentNullException>(() => new BaiduMarkupTranslationProvider(Credentials, null!));
    }

    [Fact]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        using BaiduMarkupTranslationProvider provider = new(Credentials, new HttpClient());

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsync_AsksTheEndpointThatKeepsTags()
    {
        Uri? uri = null;
        string? body = null;

        using StubHttpMessageHandler handler = Answering(
            """{"from":"en","to":"ru","trans_result":[{"src":"<b>Hello</b> world","dst":"<b>Привет</b> мир"}]}""",
            (request, sent) =>
            {
                uri = request.RequestUri;
                body = sent;
            });
        using HttpClient httpClient = new(handler);
        using BaiduMarkupTranslationProvider provider = new(Credentials, httpClient);

        MarkupTranslationResult result = await provider.ExecuteAsync(new MarkupTranslationRequest(
            new ProviderMarkup("<b>Hello</b> world"),
            new LanguageId("russian"),
            new LanguageId("english")));

        Assert.Equal("https://fanyi-api.baidu.com/ait/api/aiTextTranslate", uri!.GetLeftPart(UriPartial.Path));

        var fields = HttpUtility.ParseQueryString(body!);

        Assert.Equal("<b>Hello</b> world", fields["q"]);
        Assert.Equal("nmt", fields["model_type"]);
        Assert.Equal("1", fields["tag_handling"]);
        Assert.Equal(
            Internal.BaiduSignature.ForText("2015063000000001", "<b>Hello</b> world", fields["salt"]!, "1234567890"),
            fields["sign"]);

        Assert.Equal("<b>Привет</b> мир", result.TranslatedMarkup.Value);
        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.False(result.WasSourceLanguageDetected);
    }

    [Fact]
    public async Task ExecuteAsync_NoSource_ReportsTheDetectedOne()
    {
        using StubHttpMessageHandler handler = Answering(
            """{"from":"de","to":"ru","trans_result":[{"src":"<i>Hallo</i>","dst":"<i>Привет</i>"}]}""");
        using HttpClient httpClient = new(handler);
        using BaiduMarkupTranslationProvider provider = new(Credentials, httpClient);

        MarkupTranslationResult result = await provider.ExecuteAsync(
            new MarkupTranslationRequest(new ProviderMarkup("<i>Hallo</i>"), new LanguageId("russian")));

        Assert.Equal("german", result.SourceLanguageId.Value);
        Assert.True(result.WasSourceLanguageDetected);
    }

    [Fact]
    public async Task ExecuteAsync_RefusedByBaidu_ThrowsProviderException()
    {
        using StubHttpMessageHandler handler = Answering("""{"error_code":"59006","error_msg":"tag parse failed"}""");
        using HttpClient httpClient = new(handler);
        using BaiduMarkupTranslationProvider provider = new(Credentials, httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new MarkupTranslationRequest(new ProviderMarkup("<b>Hello"), new LanguageId("russian"))));

        Assert.Contains("59006", exception.Message, StringComparison.Ordinal);
    }
}
