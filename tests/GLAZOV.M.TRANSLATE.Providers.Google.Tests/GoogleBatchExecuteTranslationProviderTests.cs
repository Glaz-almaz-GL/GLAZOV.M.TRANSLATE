using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Abstractions.Translation;
using System.Net;
using System.Text;

namespace GLAZOV.M.TRANSLATE.Providers.Google.Tests;

public sealed class GoogleBatchExecuteTranslationProviderTests
{
    // Answers recorded from translate.google.com on 2026-10-01.
    private const string RussianToEnglish = """""
)]}'

513
[["wrb.fr","MkEWBc","[[null,null,\"ru\",[[[0,[[[null,7]],[true]]],[1,[[[null,8],[8,17]],[false,true]]]],17],null,null,[\"Привет. Как дела?\",\"auto\",\"en\",true]],[[[null,null,null,null,null,[[\"Hello.\",null,null,null,null,null,\"Привет.\",1],[\"How are you?\",null,true,null,null,null,\"Как дела?\",1]],null,null,null,[]]],\"en\",1,\"ru\",[\"Привет. Как дела?\",\"auto\",\"en\",true]],\"ru\",null,null,null,null,[[[0]],[[0]]]]",null,null,null,"generic"],["di",484],["af.httprm",484,"3724840635108442698",77]]
25
[["e",4,null,null,588]]
""""";

    private const string FourLines = """""
)]}'

815
[["wrb.fr","MkEWBc","[[null,null,null,[[[0,[[[null,12]],[true]]],[1,[[[null,13],[13,23]],[false,true]]],[2,[[[null,25],[25,42]],[false,true]]],[3,[[[null,44],[44,47]],[false,true]]]],47],null,null,[\"Good morning\\nThank you.  See you tomorrow.\\n\\nBye\",\"en\",\"fr\",true]],[[[null,null,null,null,null,[[\"Bonjour\",null,null,null,null,null,\"Good morning\",1],[\"\\nMerci.\",null,null,null,null,null,\"Thank you.\",1],[\"À demain.\",null,true,null,null,null,\"See you tomorrow.\",1],[\"\\n\\nAu revoir\",null,null,null,null,null,\"Bye\",1]],null,null,null,[]]],\"fr\",1,\"en\",[\"Good morning\\nThank you.  See you tomorrow.\\n\\nBye\",\"en\",\"fr\",true]],\"en\",null,null,null,null,[[[0],[[6,1]]],[[0],[[6,1]]],[[0]],[[0]]]]",null,null,null,"generic"],["di",822],["af.httprm",821,"-6323749315485221794",77]]
25
[["e",4,null,null,852]]
""""";

    private const string Refusal = """""
)]}'

104
[["wrb.fr","MkEWBc",null,null,null,[3],"generic"],["di",11],["af.httprm",10,"4774233274928503429",81]]
25
[["e",4,null,null,141]]
""""";

    private static HttpClient ClientAnswering(string body, HttpStatusCode status = HttpStatusCode.OK, Action<HttpRequestMessage>? inspect = null)
    {
        StubHttpMessageHandler handler = new(request =>
        {
            inspect?.Invoke(request);

            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "text/plain") };
        });

        return new HttpClient(handler);
    }

    [Fact]
    public void Name_IsDistinctFromTheOlderGoogleProvider()
    {
        using GoogleBatchExecuteTranslationProvider provider = new();

        Assert.Equal("Google.BatchExecute", provider.Name);
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new GoogleBatchExecuteTranslationProvider(null!));
    }

    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new GoogleBatchExecuteTranslationProvider(new GoogleBatchExecuteOptions(), null!));
    }

    [Fact]
    public async Task ExecuteAsync_AutoDetectedSource_ReturnsTranslationAndDetectedLanguage()
    {
        using HttpClient httpClient = ClientAnswering(RussianToEnglish);
        using GoogleBatchExecuteTranslationProvider provider = new(new GoogleBatchExecuteOptions(), httpClient);

        TextTranslationResult result = await provider.ExecuteAsync(
            new TextTranslationRequest(new ProviderText("Привет. Как дела?"), new LanguageId("english")));

        Assert.Equal("Hello. How are you?", result.TranslatedText.Value);
        Assert.Equal("russian", result.SourceLanguageId.Value);
        Assert.True(result.WasSourceLanguageDetected);
    }

    [Fact]
    public async Task ExecuteAsync_LineBreaksAndSpaces_AreKept()
    {
        using HttpClient httpClient = ClientAnswering(FourLines);
        using GoogleBatchExecuteTranslationProvider provider = new(new GoogleBatchExecuteOptions(), httpClient);

        TextTranslationResult result = await provider.ExecuteAsync(new TextTranslationRequest(
            new ProviderText("Good morning\nThank you.  See you tomorrow.\n\nBye"),
            new LanguageId("french"),
            new LanguageId("english")));

        Assert.Equal("Bonjour\nMerci. À demain.\n\nAu revoir", result.TranslatedText.Value);
        Assert.False(result.WasSourceLanguageDetected);
    }

    [Fact]
    public async Task ExecuteAsync_SendsTheCallTheWebPageSends()
    {
        string? query = null;
        string? form = null;
        string? sameDomain = null;

        using HttpClient httpClient = ClientAnswering(RussianToEnglish, inspect: request =>
        {
            query = request.RequestUri!.Query;
            form = request.Content!.ReadAsStringAsync().Result;
            sameDomain = request.Headers.GetValues("X-Same-Domain").Single();
            Assert.Equal("/_/TranslateWebserverUi/data/batchexecute", request.RequestUri.AbsolutePath);
            Assert.Equal(HttpMethod.Post, request.Method);
        });
        using GoogleBatchExecuteTranslationProvider provider = new(new GoogleBatchExecuteOptions(), httpClient);

        await provider.ExecuteAsync(new TextTranslationRequest(new ProviderText("Привет"), new LanguageId("english")));

        Assert.Contains("rpcids=MkEWBc", query);
        Assert.Contains("rt=c", query);
        Assert.Equal("1", sameDomain);

        string request = Uri.UnescapeDataString(form!.Replace("+", "%20"));

        Assert.Equal("""f.req=[[["MkEWBc","[[\"Привет\",\"auto\",\"en\",true],[null]]",null,"generic"]]]""", request);
    }

    [Fact]
    public async Task ExecuteAsync_OptionsChangeTheAddressAndTheCall()
    {
        Uri? address = null;

        using HttpClient httpClient = ClientAnswering(RussianToEnglish.Replace("MkEWBc", "XyZ123"), inspect: request => address = request.RequestUri);
        using GoogleBatchExecuteTranslationProvider provider = new(
            new GoogleBatchExecuteOptions { ServiceUrl = "https://mirror.example/", RpcId = "XyZ123", BuildLabel = "boq_x" },
            httpClient);

        await provider.ExecuteAsync(new TextTranslationRequest(new ProviderText("Привет"), new LanguageId("english")));

        Assert.Equal("mirror.example", address!.Host);
        Assert.Contains("rpcids=XyZ123", address.Query);
        Assert.Contains("bl=boq_x", address.Query);
    }

    [Fact]
    public async Task ExecuteAsync_ServiceRefusesTheCall_ThrowsProviderExceptionWithTheCode()
    {
        using HttpClient httpClient = ClientAnswering(Refusal);
        using GoogleBatchExecuteTranslationProvider provider = new(new GoogleBatchExecuteOptions(), httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() =>
            provider.ExecuteAsync(new TextTranslationRequest(new ProviderText("Hello"), new LanguageId("french"))));

        Assert.Contains("code 3", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_TooManyRequests_ThrowsProviderExceptionWithTheStatus()
    {
        using HttpClient httpClient = ClientAnswering("blocked", HttpStatusCode.TooManyRequests);
        using GoogleBatchExecuteTranslationProvider provider = new(new GoogleBatchExecuteOptions(), httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() =>
            provider.ExecuteAsync(new TextTranslationRequest(new ProviderText("Hello"), new LanguageId("french"))));

        Assert.Contains("429", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not an answer")]
    [InlineData(")]}'\n\n5\n[[1]]\n")]
    public async Task ExecuteAsync_AnswerOfUnexpectedShape_ThrowsProviderException(string body)
    {
        using HttpClient httpClient = ClientAnswering(body);
        using GoogleBatchExecuteTranslationProvider provider = new(new GoogleBatchExecuteOptions(), httpClient);

        await Assert.ThrowsAsync<ProviderException>(() =>
            provider.ExecuteAsync(new TextTranslationRequest(new ProviderText("Hello"), new LanguageId("french"))));
    }

    [Fact]
    public async Task ExecuteAsync_TextLongerThanTheLimit_ThrowsArgumentException()
    {
        using HttpClient httpClient = ClientAnswering(RussianToEnglish);
        using GoogleBatchExecuteTranslationProvider provider = new(new GoogleBatchExecuteOptions { MaxTextLength = 10 }, httpClient);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            provider.ExecuteAsync(new TextTranslationRequest(new ProviderText("Hello, world, again"), new LanguageId("french"))));
    }

    [Theory]
    [InlineData("")]
    [InlineData("translate.google.com")]
    [InlineData("ftp://translate.google.com")]
    public void Options_BadServiceUrl_ThrowsArgumentException(string url)
    {
        Assert.Throws<ArgumentException>(() => new GoogleBatchExecuteOptions { ServiceUrl = url });
    }

    [Fact]
    public void Options_NonPositiveMaxTextLength_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GoogleBatchExecuteOptions { MaxTextLength = 0 });
    }

    [LiveFact]
    public async Task ExecuteAsync_LiveGoogleTranslate_TranslatesText()
    {
        using GoogleBatchExecuteTranslationProvider provider = new();

        TextTranslationResult result = await provider.ExecuteAsync(
            new TextTranslationRequest(new ProviderText("Hello, world."), new LanguageId("russian")));

        Assert.False(string.IsNullOrWhiteSpace(result.TranslatedText.Value));
        Assert.Equal("english", result.SourceLanguageId.Value);
    }
}