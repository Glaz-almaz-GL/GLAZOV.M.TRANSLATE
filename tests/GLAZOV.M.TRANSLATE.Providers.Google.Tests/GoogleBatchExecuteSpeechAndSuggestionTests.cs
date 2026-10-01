using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Abstractions.TextToSpeech;
using System.Net;
using System.Text;

namespace GLAZOV.M.TRANSLATE.Providers.Google.Tests;

public sealed class GoogleBatchExecuteSpeechAndSuggestionTests
{
    // The audio is three bytes of an MP3 frame header, written the way the service writes it.
    private const string SpeechAnswer = """
)]}'

86
[["wrb.fr","jQ1olc","[\"//OE\"]",null,null,null,"generic"],["di",100],["af.httprm",100,"1",1]]
25
[["e",4,null,null,141]]
""";

    // Recorded from translate.google.com on 2026-10-01.
    private const string SuggestionAnswer = """
)]}'

247
[["wrb.fr","AVdN8","[[[\"hello\",\"привет\"],[\"hello how are you\",\"Привет, как дела?\"],[\"hello how are you doing\",\"Привет, как поживаешь?\"]],\"en\",\"ru\"]",null,null,null,"generic"],["di",146],["af.httprm",146,"1933831618164904077",86]]
25
[["e",4,null,null,320]]
""";

    private const string NoSuggestions = """
)]}'

100
[["wrb.fr","AVdN8","[]",null,null,null,"generic"],["di",10],["af.httprm",10,"1",1]]
25
[["e",4,null,null,141]]
""";

    private const string Refusal = """
)]}'

104
[["wrb.fr","jQ1olc",null,null,null,[3],"generic"],["di",11],["af.httprm",10,"4774233274928503429",81]]
25
[["e",4,null,null,141]]
""";

    private static HttpClient ClientAnswering(string body, Action<HttpRequestMessage, string>? inspect = null)
    {
        StubHttpMessageHandler handler = new(request =>
        {
            inspect?.Invoke(request, request.Content!.ReadAsStringAsync().Result);

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "text/plain") };
        });

        return new HttpClient(handler);
    }

    [Fact]
    public async Task Speech_ReturnsTheDecodedAudio()
    {
        string? form = null;
        string? query = null;

        using HttpClient httpClient = ClientAnswering(SpeechAnswer, (request, body) =>
        {
            form = Uri.UnescapeDataString(body.Replace("+", "%20"));
            query = request.RequestUri!.Query;
        });
        using GoogleBatchExecuteTextToSpeechProvider provider = new(new GoogleBatchExecuteOptions(), httpClient);

        TextToSpeechResult result = await provider.ExecuteAsync(
            new TextToSpeechRequest(new ProviderText("Hello world"), new LanguageId("english")));

        Assert.Equal(new byte[] { 0xFF, 0xF3, 0x84 }, result.AudioData.ToArray());
        Assert.Equal("audio/mpeg", result.ContentType.Value);
        Assert.Equal("english", result.LanguageId.Value);
        Assert.Contains("rpcids=jQ1olc", query);
        Assert.Equal("""f.req=[[["jQ1olc","[\"Hello world\",\"en\",null,\"null\"]",null,"generic"]]]""", form);
    }

    [Fact]
    public async Task Speech_LongText_IsCutIntoPiecesAndTheAudioJoinedInOrder()
    {
        int calls = 0;

        using HttpClient httpClient = ClientAnswering(SpeechAnswer, (_, _) => Interlocked.Increment(ref calls));
        using GoogleBatchExecuteTextToSpeechProvider provider = new(new GoogleBatchExecuteOptions { MaxSpeechChunkLength = 10 }, httpClient);

        TextToSpeechResult result = await provider.ExecuteAsync(
            new TextToSpeechRequest(new ProviderText("one two three four five six"), new LanguageId("english")));

        Assert.True(calls > 1);
        Assert.Equal(calls * 3, result.AudioData.Length);
    }

    [Fact]
    public async Task Speech_NamedVoice_IsRefused()
    {
        using HttpClient httpClient = ClientAnswering(SpeechAnswer);
        using GoogleBatchExecuteTextToSpeechProvider provider = new(new GoogleBatchExecuteOptions(), httpClient);

        await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new TextToSpeechRequest(new ProviderText("Hello"), new LanguageId("english"), new VoiceName("anna"))));
    }

    [Fact]
    public async Task Speech_ServiceRefuses_ThrowsProviderExceptionWithTheCode()
    {
        using HttpClient httpClient = ClientAnswering(Refusal);
        using GoogleBatchExecuteTextToSpeechProvider provider = new(new GoogleBatchExecuteOptions(), httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ExecuteAsync(
            new TextToSpeechRequest(new ProviderText("Hello"), new LanguageId("english"))));

        Assert.Contains("code 3", exception.Message);
    }

    [Fact]
    public async Task Speech_OptionsChangeTheCallName()
    {
        string? query = null;

        using HttpClient httpClient = ClientAnswering(SpeechAnswer.Replace("jQ1olc", "Zz9"), (request, _) => query = request.RequestUri!.Query);
        using GoogleBatchExecuteTextToSpeechProvider provider = new(new GoogleBatchExecuteOptions { SpeechRpcId = "Zz9" }, httpClient);

        await provider.ExecuteAsync(new TextToSpeechRequest(new ProviderText("Hello"), new LanguageId("english")));

        Assert.Contains("rpcids=Zz9", query);
    }

    [Fact]
    public async Task Suggestions_ReturnPhrasesWithTranslations()
    {
        string? form = null;

        using HttpClient httpClient = ClientAnswering(SuggestionAnswer, (_, body) => form = Uri.UnescapeDataString(body.Replace("+", "%20")));
        using GoogleSuggestionProvider provider = new(new GoogleBatchExecuteOptions(), httpClient);

        var suggestions = await provider.SuggestAsync(new ProviderText("Hello"), new LanguageId("english"), new LanguageId("russian"));

        Assert.Equal(3, suggestions.Length);
        Assert.Equal(new GoogleSuggestion("hello", "привет"), suggestions[0]);
        Assert.Equal("Привет, как дела?", suggestions[1].Translation);
        Assert.Equal("""f.req=[[["AVdN8","[\"Hello\",\"en\",\"ru\"]",null,"generic"]]]""", form);
    }

    [Fact]
    public async Task Suggestions_NothingToSuggest_ReturnsAnEmptyList()
    {
        using HttpClient httpClient = ClientAnswering(NoSuggestions);
        using GoogleSuggestionProvider provider = new(new GoogleBatchExecuteOptions(), httpClient);

        var suggestions = await provider.SuggestAsync(new ProviderText("zzz"), new LanguageId("english"), new LanguageId("russian"));

        Assert.Empty(suggestions);
    }

    [Fact]
    public async Task Suggestions_NullArguments_ThrowArgumentNullException()
    {
        using GoogleSuggestionProvider provider = new();

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.SuggestAsync(null!, new LanguageId("english"), new LanguageId("russian")));
        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.SuggestAsync(new ProviderText("a"), null!, new LanguageId("russian")));
        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.SuggestAsync(new ProviderText("a"), new LanguageId("english"), null!));
    }

    [Fact]
    public void Options_NewSettings_AreValidated()
    {
        Assert.Throws<ArgumentException>(() => new GoogleBatchExecuteOptions { SpeechRpcId = " " });
        Assert.Throws<ArgumentException>(() => new GoogleBatchExecuteOptions { SuggestionsRpcId = "" });
        Assert.Throws<ArgumentOutOfRangeException>(() => new GoogleBatchExecuteOptions { MaxSpeechChunkLength = 0 });
    }

    [LiveFact]
    public async Task Speech_LiveGoogleTranslate_SpeaksEnglish()
    {
        using GoogleBatchExecuteTextToSpeechProvider provider = new();

        TextToSpeechResult result = await provider.ExecuteAsync(
            new TextToSpeechRequest(new ProviderText("Hello world, this is a test."), new LanguageId("english")));

        Assert.True(result.AudioData.Length > 1000);
    }

    [LiveFact]
    public async Task Suggestions_LiveGoogleTranslate_SuggestsPhrases()
    {
        using GoogleSuggestionProvider provider = new();

        var suggestions = await provider.SuggestAsync(new ProviderText("Hello"), new LanguageId("english"), new LanguageId("russian"));

        Assert.NotEmpty(suggestions);
    }
}
