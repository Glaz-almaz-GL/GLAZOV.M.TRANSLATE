using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Abstractions.Translation;
using GLAZOV.M.TRANSLATE.Abstractions.Transliteration;
using System.Net;
using System.Text;

namespace GLAZOV.M.TRANSLATE.Providers.Papago.Tests;

public sealed class PapagoProvidersTests
{
    // Answers recorded from papago.naver.com on 2026-10-01; the list of language
    // guesses is cut to its first entries.
    private const string EnglishToKorean = """{"delay":400,"delaySmt":400,"srcLangType":"en","tarLangType":"ko","translatedText":"안녕 세계. 어떻게 지내세요?","engineType":"PRETRANS","tlit":{"message":{"tlitResult":[{"token":"안녕","phoneme":"annyong"},{"token":"세계.","phoneme":"segye"},{"token":"어떻게","phoneme":"ottoke"},{"token":"지내세요?","phoneme":"jinaeseyo"}]}},"langDetection":{"nbests":[{"lang":"en","prob":0.9410652479947839},{"lang":"de","prob":0.03141801492900103},{"lang":"unk","prob":0.0}]},"replaceInfos":[]}""";

    private const string JapaneseToRussian = """{"delay":400,"delaySmt":400,"srcLangType":"ja","tarLangType":"ru","translatedText":"Привет, боже мой.\nВы здоровы?","engineType":"N2MT","tlitSrc":{"message":{"tlitResult":[{"token":"こんにちは","phoneme":"kon'nichiwa"},{"token":"世界","phoneme":"sekai"},{"token":"お元気ですか","phoneme":"ogen'kidesuka"}]}},"langDetection":{"nbests":[{"lang":"ja","prob":0.9995870457623176},{"lang":"zh-TW","prob":3.3309806609702774E-4},{"lang":"unk","prob":0.0}]},"replaceInfos":[]}""";

    private const string UnsupportedTarget = """{"errorCode":"60104","errorMessage":"Unsupported target language"}""";

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
    public void Name_ReturnsPapago()
    {
        using PapagoTranslationProvider provider = new();

        Assert.Equal("Papago", provider.Name);
    }

    [Fact]
    public void Constructors_NullArguments_ThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PapagoTranslationProvider(null!));
        Assert.Throws<ArgumentNullException>(() => new PapagoTranslationProvider(new PapagoOptions(), null!));
        Assert.Throws<ArgumentNullException>(() => new PapagoTransliterationProvider(null!));
        Assert.Throws<ArgumentNullException>(() => new PapagoTransliterationProvider(new PapagoOptions(), null!));
    }

    [Fact]
    public async Task Translation_ExplicitSource_ReturnsTranslationWithoutDetectionFlag()
    {
        using HttpClient httpClient = ClientAnswering(EnglishToKorean);
        using PapagoTranslationProvider provider = new(new PapagoOptions(), httpClient);

        TextTranslationResult result = await provider.ExecuteAsync(new TextTranslationRequest(
            new ProviderText("Hello world. How are you?"),
            new LanguageId("korean"),
            new LanguageId("english")));

        Assert.Equal("안녕 세계. 어떻게 지내세요?", result.TranslatedText.Value);
        Assert.Equal("english", result.SourceLanguageId.Value);
        Assert.False(result.WasSourceLanguageDetected);
    }

    [Fact]
    public async Task Translation_AutoDetectedSource_ReturnsDetectedLanguage()
    {
        using HttpClient httpClient = ClientAnswering(JapaneseToRussian);
        using PapagoTranslationProvider provider = new(new PapagoOptions(), httpClient);

        TextTranslationResult result = await provider.ExecuteAsync(
            new TextTranslationRequest(new ProviderText("こんにちは世界\nお元気ですか"), new LanguageId("russian")));

        Assert.Equal("Привет, боже мой.\nВы здоровы?", result.TranslatedText.Value);
        Assert.Equal("japanese", result.SourceLanguageId.Value);
        Assert.True(result.WasSourceLanguageDetected);
    }

    [Fact]
    public async Task Translation_SendsTheFormTheWebPageSends()
    {
        Uri? address = null;
        string? form = null;
        string? origin = null;

        using HttpClient httpClient = ClientAnswering(EnglishToKorean, inspect: request =>
        {
            address = request.RequestUri;
            form = request.Content!.ReadAsStringAsync().Result;
            origin = request.Headers.GetValues("Origin").Single();
        });
        using PapagoTranslationProvider provider = new(new PapagoOptions(), httpClient);

        await provider.ExecuteAsync(new TextTranslationRequest(new ProviderText("Hello world"), new LanguageId("korean")));

        Assert.Equal("/api/text/translation", address!.AbsolutePath);
        Assert.Equal("https://papago.naver.com", origin);
        Assert.Contains("source=auto", form);
        Assert.Contains("target=ko", form);
        Assert.Contains("text=Hello+world", form);
        Assert.Contains("locale=en", form);
        Assert.Contains("deviceId=", form);
    }

    [Fact]
    public async Task Translation_Chinese_IsAskedForInTheSimplifiedScript()
    {
        string? form = null;

        using HttpClient httpClient = ClientAnswering(EnglishToKorean, inspect: request => form = request.Content!.ReadAsStringAsync().Result);
        using PapagoTranslationProvider provider = new(new PapagoOptions(), httpClient);

        await provider.ExecuteAsync(new TextTranslationRequest(new ProviderText("Hello"), new LanguageId("chinese")));

        Assert.Contains("target=zh-CN", form);
    }

    [Fact]
    public async Task Translation_OptionsChangeTheAddressAndTheLocale()
    {
        Uri? address = null;
        string? form = null;

        using HttpClient httpClient = ClientAnswering(EnglishToKorean, inspect: request =>
        {
            address = request.RequestUri;
            form = request.Content!.ReadAsStringAsync().Result;
        });
        using PapagoTranslationProvider provider = new(
            new PapagoOptions { ServiceUrl = "https://mirror.example/", InterfaceLocale = "ko" },
            httpClient);

        await provider.ExecuteAsync(new TextTranslationRequest(new ProviderText("Hello"), new LanguageId("korean")));

        Assert.Equal("mirror.example", address!.Host);
        Assert.Contains("locale=ko", form);
    }

    [Fact]
    public async Task Translation_ServiceRefuses_ThrowsProviderExceptionWithTheReason()
    {
        using HttpClient httpClient = ClientAnswering(UnsupportedTarget, HttpStatusCode.BadRequest);
        using PapagoTranslationProvider provider = new(new PapagoOptions(), httpClient);

        ProviderException exception = await Assert.ThrowsAsync<ProviderException>(() =>
            provider.ExecuteAsync(new TextTranslationRequest(new ProviderText("Hello"), new LanguageId("korean"))));

        Assert.Contains("400", exception.Message);
        Assert.Contains("Unsupported target language", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{}")]
    public async Task Translation_AnswerOfUnexpectedShape_ThrowsProviderException(string body)
    {
        using HttpClient httpClient = ClientAnswering(body);
        using PapagoTranslationProvider provider = new(new PapagoOptions(), httpClient);

        await Assert.ThrowsAsync<ProviderException>(() =>
            provider.ExecuteAsync(new TextTranslationRequest(new ProviderText("Hello"), new LanguageId("korean"))));
    }

    [Fact]
    public async Task Translation_TextLongerThanTheLimit_ThrowsArgumentException()
    {
        using HttpClient httpClient = ClientAnswering(EnglishToKorean);
        using PapagoTranslationProvider provider = new(new PapagoOptions { MaxTextLength = 5 }, httpClient);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            provider.ExecuteAsync(new TextTranslationRequest(new ProviderText("Hello world"), new LanguageId("korean"))));
    }

    [Fact]
    public async Task Transliteration_ReturnsTheSourceRendering()
    {
        string? form = null;

        using HttpClient httpClient = ClientAnswering(JapaneseToRussian, inspect: request => form = request.Content!.ReadAsStringAsync().Result);
        using PapagoTransliterationProvider provider = new(new PapagoOptions(), httpClient);

        TransliterationResult result = await provider.ExecuteAsync(
            new TransliterationRequest(new ProviderText("こんにちは世界 お元気ですか")));

        Assert.Equal("kon'nichiwa sekai ogen'kidesuka", result.Transliteration.Value);
        Assert.Equal("japanese", result.LanguageId.Value);
        Assert.True(result.WasLanguageDetected);
        Assert.Contains("target=en", form);
    }

    [Fact]
    public async Task Transliteration_TextAlreadyInLatin_ThrowsProviderException()
    {
        using HttpClient httpClient = ClientAnswering("""{"srcLangType":"en","tarLangType":"en","translatedText":"Hello"}""");
        using PapagoTransliterationProvider provider = new(new PapagoOptions(), httpClient);

        await Assert.ThrowsAsync<ProviderException>(() =>
            provider.ExecuteAsync(new TransliterationRequest(new ProviderText("Hello"))));
    }

    [Theory]
    [InlineData("")]
    [InlineData("papago.naver.com")]
    [InlineData("ftp://papago.naver.com")]
    public void Options_BadServiceUrl_ThrowsArgumentException(string url)
    {
        Assert.Throws<ArgumentException>(() => new PapagoOptions { ServiceUrl = url });
    }

    [Fact]
    public void Options_NonPositiveMaxTextLength_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PapagoOptions { MaxTextLength = 0 });
    }

    [LiveFact]
    public async Task Translation_LivePapago_TranslatesText()
    {
        using PapagoTranslationProvider provider = new();

        TextTranslationResult result = await provider.ExecuteAsync(
            new TextTranslationRequest(new ProviderText("Hello, world."), new LanguageId("korean")));

        Assert.False(string.IsNullOrWhiteSpace(result.TranslatedText.Value));
        Assert.Equal("english", result.SourceLanguageId.Value);
    }

    [LiveFact]
    public async Task Transliteration_LivePapago_RomanizesJapanese()
    {
        using PapagoTransliterationProvider provider = new();

        TransliterationResult result = await provider.ExecuteAsync(
            new TransliterationRequest(new ProviderText("こんにちは"), new LanguageId("japanese")));

        Assert.Contains("nichiwa", result.Transliteration.Value);
    }
}
