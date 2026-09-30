using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Providers.Common;

namespace GLTranslate.Providers.Baidu.Internal;

/// <summary>
/// Translates between the languages of GLTranslate and the language codes of
/// Baidu.
/// </summary>
/// <remarks>
/// Baidu writes some languages as their ISO 639-1 code and a good number as
/// something of its own — <c>jp</c> for Japanese, <c>kor</c> for Korean,
/// <c>fra</c> for French — which is what the table below records.
/// </remarks>
internal static class BaiduLanguageCodeResolver
{
    // ISO 639-1 code -> Baidu code, for every language Baidu does not write as
    // its ISO 639-1 code.
    private static readonly KeyValuePair<string, string>[] Differing =
    [
        new("af", "afr"), new("am", "amh"), new("ar", "ara"), new("as", "asm"), new("ay", "aym"),
        new("az", "aze"), new("ba", "bak"), new("be", "bel"), new("bg", "bul"), new("bn", "ben"),
        new("br", "bre"), new("bs", "bos"), new("ca", "cat"), new("co", "cos"), new("cy", "wel"),
        new("da", "dan"), new("eo", "epo"), new("es", "spa"), new("et", "est"), new("eu", "baq"),
        new("fa", "per"), new("fi", "fin"), new("fr", "fra"), new("fy", "fry"), new("ga", "gle"),
        new("gd", "gla"), new("gl", "glg"), new("gu", "guj"), new("ha", "hau"), new("he", "heb"),
        new("hr", "hrv"), new("hy", "arm"), new("ig", "ibo"), new("is", "ice"), new("ja", "jp"),
        new("jv", "jav"), new("ka", "geo"), new("kn", "kan"), new("ko", "kor"), new("ku", "kur"),
        new("ky", "kir"), new("la", "lat"), new("lb", "ltz"), new("lo", "lao"), new("lt", "lit"),
        new("lv", "lav"), new("mi", "mao"), new("mk", "mac"), new("ml", "mal"), new("mr", "mar"),
        new("ms", "may"), new("mt", "mlt"), new("my", "bur"), new("ne", "nep"), new("no", "nor"),
        new("ny", "nya"), new("oc", "oci"), new("or", "ori"), new("pa", "pan"), new("ps", "pus"),
        new("ro", "rom"), new("rw", "kin"), new("sa", "san"), new("sd", "snd"), new("si", "sin"),
        new("sl", "slo"), new("sn", "sna"), new("so", "som"), new("sq", "alb"), new("sr", "srp"),
        new("st", "sot"), new("su", "sun"), new("sv", "swe"), new("sw", "swa"), new("ta", "tam"),
        new("te", "tel"), new("tg", "tgk"), new("tk", "tuk"), new("tl", "tgl"), new("tt", "tat"),
        new("uk", "ukr"), new("ur", "urd"), new("vi", "vie"), new("wo", "wol"), new("xh", "xho"),
        new("yi", "yid"), new("yo", "yor"), new("zu", "zul"),
    ];

    private static readonly LanguageCodeResolver Resolver = new(
        BaiduProvider.Name,
        Differing,
        [.. Differing.Select(pair => new KeyValuePair<string, string>(pair.Value, pair.Key))]);

    /// <summary>
    /// Gets the Baidu code of a language.
    /// </summary>
    /// <param name="languageId">
    /// The language.
    /// </param>
    /// <returns>
    /// The code Baidu knows the language by.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="languageId"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="Abstractions.Providers.ProviderException">
    /// Thrown when the language is unknown to GLTranslate or has no ISO 639-1
    /// code.
    /// </exception>
    public static string ToBaiduCode(LanguageId languageId)
    {
        return Resolver.ToProviderCode(languageId);
    }

    /// <summary>
    /// Gets the language a Baidu code stands for.
    /// </summary>
    /// <param name="baiduLanguageCode">
    /// The code Baidu answered with.
    /// </param>
    /// <returns>
    /// The language.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="baiduLanguageCode"/> is empty or consists
    /// only of white-space characters.
    /// </exception>
    /// <exception cref="Abstractions.Providers.ProviderException">
    /// Thrown when the code is not known to GLTranslate.
    /// </exception>
    public static LanguageId FromBaiduCode(string baiduLanguageCode)
    {
        return Resolver.FromProviderCode(baiduLanguageCode);
    }
}
