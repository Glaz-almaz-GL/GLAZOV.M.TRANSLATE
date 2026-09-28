using System.Collections.Immutable;

namespace GLTranslate.Providers.Microsoft.Internal;

/// <summary>
/// Holds the default voice of every language the Microsoft speech endpoint
/// speaks.
/// </summary>
/// <remarks>
/// The endpoint speaks a named voice, not a language, so a language has to be
/// turned into one. These are the voices the Microsoft Translator application
/// uses by default; a language absent from this table cannot be spoken.
/// </remarks>
internal static class MicrosoftVoices
{
    private static readonly ImmutableDictionary<string, MicrosoftVoice> ByIso6391 =
        new Dictionary<string, MicrosoftVoice>(StringComparer.Ordinal)
        {
        ["af"] = new("af-ZA-AdriNeural", "Female", "af-ZA"),
        ["am"] = new("am-ET-MekdesNeural", "Female", "am-ET"),
        ["ar"] = new("ar-SA-HamedNeural", "Male", "ar-SA"),
        ["bg"] = new("bg-BG-BorislavNeural", "Male", "bg-BG"),
        ["bn"] = new("bn-IN-TanishaaNeural", "Female", "br-IN"),
        ["ca"] = new("ca-ES-JoanaNeural", "Female", "ca-ES"),
        ["cs"] = new("cs-CZ-AntoninNeural", "Male", "cs-CZ"),
        ["cy"] = new("cy-GB-NiaNeural", "Female", "cy-GB"),
        ["da"] = new("da-DK-ChristelNeural", "Female", "da-DK"),
        ["de"] = new("de-DE-KatjaNeural", "Female", "de-DE"),
        ["el"] = new("el-GR-NestorasNeural", "Male", "el-GR"),
        ["en"] = new("en-US-AriaNeural", "Female", "en-US"),
        ["es"] = new("es-ES-ElviraNeural", "Female", "es-ES"),
        ["et"] = new("et-EE-AnuNeural", "Female", "et-EE"),
        ["fa"] = new("fa-IR-DilaraNeural", "Female", "fa-IR"),
        ["fi"] = new("fi-FI-NooraNeural", "Female", "fi-FI"),
        ["fr"] = new("fr-FR-DeniseNeural", "Female", "fr-FR"),
        ["ga"] = new("ga-IE-OrlaNeural", "Female", "ga-IE"),
        ["gu"] = new("gu-IN-DhwaniNeural", "Female", "gu-IN"),
        ["he"] = new("he-IL-AvriNeural", "Male", "he-IL"),
        ["hi"] = new("hi-IN-SwaraNeural", "Female", "hi-IN"),
        ["hr"] = new("hr-HR-SreckoNeural", "Male", "hr-HR"),
        ["hu"] = new("hu-HU-TamasNeural", "Male", "hu-HU"),
        ["id"] = new("id-ID-ArdiNeural", "Male", "id-ID"),
        ["is"] = new("is-IS-GudrunNeural", "Female", "is-IS"),
        ["it"] = new("it-IT-DiegoNeural", "Male", "it-IT"),
        ["ja"] = new("ja-JP-NanamiNeural", "Female", "ja-JP"),
        ["kk"] = new("kk-KZ-AigulNeural", "Female", "kk-KZ"),
        ["km"] = new("km-KH-SreymomNeural", "Female", "km-KH"),
        ["kn"] = new("kn-IN-SapnaNeural", "Female", "kn-IN"),
        ["ko"] = new("ko-KR-SunHiNeural", "Female", "ko-KR"),
        ["lo"] = new("lo-LA-KeomanyNeural", "Female", "lo-LA"),
        ["lv"] = new("lv-LV-EveritaNeural", "Female", "lv-LV"),
        ["lt"] = new("lt-LT-OnaNeural", "Female", "lt-LT"),
        ["mk"] = new("mk-MK-MarijaNeural", "Female", "mk-MK"),
        ["ml"] = new("ml-IN-SobhanaNeural", "Female", "ml-IN"),
        ["mr"] = new("mr-IN-AarohiNeural", "Female", "mr-IN"),
        ["ms"] = new("ms-MY-OsmanNeural", "Male", "ms-MY"),
        ["mt"] = new("mt-MT-GraceNeural", "Female", "mt-MT"),
        ["my"] = new("my-MM-NilarNeural", "Female", "my-MM"),
        ["nl"] = new("nl-NL-ColetteNeural", "Female", "nl-NL"),
        ["no"] = new("nb-NO-PernilleNeural", "Female", "nb-NO"),
        ["pl"] = new("pl-PL-ZofiaNeural", "Female", "pl-PL"),
        ["ps"] = new("ps-AF-LatifaNeural", "Female", "ps-AF"),
        ["pt"] = new("pt-BR-FranciscaNeural", "Female", "pt-BR"),
        ["ro"] = new("ro-RO-EmilNeural", "Male", "ro-RO"),
        ["ru"] = new("ru-RU-DariyaNeural", "Female", "ru-RU"),
        ["sk"] = new("sk-SK-LukasNeural", "Male", "sk-SK"),
        ["sl"] = new("sl-SI-RokNeural", "Male", "sl-SI"),
        ["sr"] = new("sr-RS-SophieNeural", "Female", "sr-RS"),
        ["sv"] = new("sv-SE-SofieNeural", "Female", "sv-SE"),
        ["ta"] = new("ta-IN-PallaviNeural", "Female", "ta-IN"),
        ["te"] = new("te-IN-ShrutiNeural", "Male", "te-IN"),
        ["th"] = new("th-TH-NiwatNeural", "Male", "th-TH"),
        ["tr"] = new("tr-TR-EmelNeural", "Female", "tr-TR"),
        ["uk"] = new("uk-UA-PolinaNeural", "Female", "uk-UA"),
        ["ur"] = new("ur-IN-GulNeural", "Female", "ur-IN"),
        ["uz"] = new("uz-UZ-MadinaNeural", "Female", "uz-UZ"),
        ["vi"] = new("vi-VN-NamMinhNeural", "Male", "vi-VN"),
        ["zh"] = new("zh-CN-XiaoxiaoNeural", "Female", "zh-CN"),
        }.ToImmutableDictionary();

    /// <summary>
    /// Attempts to get the voice of the specified language.
    /// </summary>
    /// <param name="iso6391Code">
    /// The ISO 639-1 code of the language.
    /// </param>
    /// <param name="voice">
    /// When this method returns <see langword="true"/>, contains the voice of
    /// the language.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the endpoint speaks the language;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool TryGet(string iso6391Code, out MicrosoftVoice voice)
    {
        return ByIso6391.TryGetValue(iso6391Code, out voice);
    }
}
