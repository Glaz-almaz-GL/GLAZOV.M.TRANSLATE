using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using System.Net;
using System.Text;

namespace GLAZOV.M.TRANSLATE.Providers.Papago.Tests;

public sealed class PapagoDictionaryProviderTests
{
    // Recorded from papago.naver.com on 2026-10-01; the list of example sentences
    // and the fields this library does not read are cut.
    private const string Hello = """{"items":[{"entry":"<b>hello</b>","subEntry":null,"matchType":"exact:entry","phoneticSigns":[{"type":"미국식","sign":"həˈloʊ"}],"pos":[{"type":"EXCLAM.","meanings":[{"meaning":"여보, 이봐; 어이","examples":[{"text":"<b>Hello</b>, this is Jane speaking.","translatedText":"여보세요, 제인입니다."}],"originalMeaning":"여보, 이봐; 어이"}]},{"type":"Noun","meanings":[{"meaning":"<b>hello</b>라고 말하기","examples":[],"originalMeaning":"x"}]}],"source":"Dong-a's Prime English-Korean Dictionary","url":"https://en.dict.naver.com/#/entry/enko/d98f52f3b6c74cc48efceb611518852f","locale":"en"}],"examples":[],"isWordType":true}""";

    private const string Nothing = """{"items":[],"examples":null,"isWordType":false}""";

    private static HttpClient ClientAnswering(string body, HttpStatusCode status = HttpStatusCode.OK, Action<HttpRequestMessage>? inspect = null)
    {
        StubHttpMessageHandler handler = new(request =>
        {
            inspect?.Invoke(request);

            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        });

        return new HttpClient(handler);
    }

    [Fact]
    public async Task LookUp_ReturnsTheEntryWithItsParts()
    {
        using HttpClient httpClient = ClientAnswering(Hello);
        using PapagoDictionaryProvider provider = new(new PapagoOptions(), httpClient);

        var entries = await provider.LookUpAsync(new ProviderText("hello"), new LanguageId("english"), new LanguageId("korean"));

        PapagoDictionaryEntry entry = Assert.Single(entries);

        Assert.Equal("hello", entry.Headword);
        Assert.Equal("Dong-a's Prime English-Korean Dictionary", entry.DictionaryName);
        Assert.Equal("en.dict.naver.com", entry.Address!.Host);
        Assert.Equal(new PapagoPhoneticSign("미국식", "həˈloʊ"), Assert.Single(entry.PhoneticSigns));
        Assert.Equal(["EXCLAM.", "Noun"], entry.PartsOfSpeech.Select(part => part.Name));

        PapagoMeaning first = Assert.Single(entry.PartsOfSpeech[0].Meanings);

        Assert.Equal("여보, 이봐; 어이", first.Text);
        Assert.Equal(new PapagoExample("Hello, this is Jane speaking.", "여보세요, 제인입니다."), Assert.Single(first.Examples));
        Assert.Equal("hello라고 말하기", entry.PartsOfSpeech[1].Meanings[0].Text);
        Assert.Empty(entry.PartsOfSpeech[1].Meanings[0].Examples);
    }

    [Fact]
    public async Task LookUp_UnknownWord_ReturnsAnEmptyList()
    {
        using HttpClient httpClient = ClientAnswering(Nothing);
        using PapagoDictionaryProvider provider = new(new PapagoOptions(), httpClient);

        var entries = await provider.LookUpAsync(new ProviderText("zzzqqq"), new LanguageId("english"), new LanguageId("korean"));

        Assert.Empty(entries);
    }

    [Fact]
    public async Task LookUp_AsksTheServiceTheWayTheWebPageDoes()
    {
        Uri? address = null;
        HttpMethod? method = null;

        using HttpClient httpClient = ClientAnswering(Hello, inspect: request =>
        {
            address = request.RequestUri;
            method = request.Method;
        });
        using PapagoDictionaryProvider provider = new(new PapagoOptions { DictionaryEntryLimit = 5 }, httpClient);

        await provider.LookUpAsync(new ProviderText("hello world"), new LanguageId("english"), new LanguageId("korean"));

        Assert.Equal(HttpMethod.Get, method);
        Assert.Equal("/api/dictionary/search", address!.AbsolutePath);
        Assert.Contains("text=hello%20world", address.Query);
        Assert.Contains("source=en", address.Query);
        Assert.Contains("target=ko", address.Query);
        Assert.Contains("clientType=WEB", address.Query);
        Assert.Contains("dictDisplay=5", address.Query);
    }

    [Fact]
    public async Task LookUp_ServiceRefuses_ThrowsProviderException()
    {
        using HttpClient httpClient = ClientAnswering("""{"errorCode":"-10001","errorMessage":"INVALID_REQUEST"}""", HttpStatusCode.BadRequest);
        using PapagoDictionaryProvider provider = new(new PapagoOptions(), httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() =>
            provider.LookUpAsync(new ProviderText("hello"), new LanguageId("english"), new LanguageId("korean")));

        Assert.Contains("INVALID_REQUEST", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("""{"items":5}""")]
    public async Task LookUp_AnswerOfUnexpectedShape_ThrowsProviderException(string body)
    {
        using HttpClient httpClient = ClientAnswering(body);
        using PapagoDictionaryProvider provider = new(new PapagoOptions(), httpClient);

        await Assert.ThrowsAsync<ProviderException>(() =>
            provider.LookUpAsync(new ProviderText("hello"), new LanguageId("english"), new LanguageId("korean")));
    }

    [Fact]
    public async Task LookUp_NullArguments_ThrowArgumentNullException()
    {
        using PapagoDictionaryProvider provider = new();

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.LookUpAsync(null!, new LanguageId("english"), new LanguageId("korean")));
        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.LookUpAsync(new ProviderText("a"), null!, new LanguageId("korean")));
        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.LookUpAsync(new ProviderText("a"), new LanguageId("english"), null!));
    }

    [Fact]
    public void Options_NonPositiveDictionaryEntryLimit_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PapagoOptions { DictionaryEntryLimit = 0 });
    }

    [LiveFact]
    public async Task LookUp_LivePapago_FindsHello()
    {
        using PapagoDictionaryProvider provider = new();

        var entries = await provider.LookUpAsync(new ProviderText("hello"), new LanguageId("english"), new LanguageId("korean"));

        Assert.NotEmpty(entries);
        Assert.NotEmpty(entries[0].PartsOfSpeech);
    }
}
