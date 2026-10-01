using GLAZOV.M.TRANSLATE.Providers.Microsoft.Internal;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GLAZOV.M.TRANSLATE.Providers.Microsoft.Generator;

/// <summary>
/// One-shot code generator that asks the Microsoft speech endpoint which
/// voices it has and writes the generated <c>MicrosoftVoices.g.cs</c> the
/// text-to-speech provider chooses a default voice from.
/// </summary>
/// <remarks>
/// This is a dev-time tool, not shipped with the library. Re-run it whenever
/// the voices need to be refreshed; Microsoft adds and retires them.
/// </remarks>
internal static class Program
{
    private const string TokenUrl = "dev.microsofttranslator.com/apps/endpoint?api-version=1.0";

    private static readonly string ProviderRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "GLAZOV.M.TRANSLATE.Providers.Microsoft"));

    private static async Task Main()
    {
        using HttpClient httpClient = new();

        (string token, string region) = await GetTokenAsync(httpClient);

        Voice[] voices = await GetVoicesAsync(httpClient, token, region);

        Dictionary<string, Voice> defaults = ChooseDefaults(voices);

        string path = Path.Combine(ProviderRoot, "Internal", "MicrosoftVoices.g.cs");

        await File.WriteAllTextAsync(path, Generate(defaults), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        Console.WriteLine($"{voices.Length} voices, {defaults.Count} languages -> {path}");
    }

    private sealed record Voice(string ShortName, string Locale, string LocaleName, string Status, string VoiceType);

    private static async Task<(string Token, string Region)> GetTokenAsync(HttpClient httpClient)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, new Uri($"https://{TokenUrl}"));

        request.Headers.Add("X-ClientVersion", "N/A");
        request.Headers.Add("X-MT-Signature", MicrosoftSignature.Create(TokenUrl));
        request.Headers.Add("X-UserId", "0");

        using HttpResponseMessage response = await httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return (document.RootElement.GetProperty("t").GetString()!, document.RootElement.GetProperty("r").GetString()!);
    }

    private static async Task<Voice[]> GetVoicesAsync(HttpClient httpClient, string token, string region)
    {
        using HttpRequestMessage request = new(
            HttpMethod.Get,
            new Uri($"https://{region}.tts.speech.microsoft.com/cognitiveservices/voices/list"));

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using HttpResponseMessage response = await httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return [.. document.RootElement.EnumerateArray().Select(voice => new Voice(
            voice.GetProperty("ShortName").GetString()!,
            voice.GetProperty("Locale").GetString()!,
            voice.GetProperty("LocaleName").GetString()!,
            voice.GetProperty("Status").GetString() ?? string.Empty,
            voice.GetProperty("VoiceType").GetString() ?? string.Empty))];
    }

    // Curated: the locale to speak a language in when it is spoken in many and
    // the rule below cannot tell which - there is no en-EN or zh-ZH. Everything
    // absent from here is decided by the rule.
    private static readonly Dictionary<string, string> PreferredLocales = new(StringComparer.Ordinal)
    {
        ["ar"] = "ar-SA",
        ["en"] = "en-US",
        ["es"] = "es-ES",
        ["fa"] = "fa-IR",
        ["hi"] = "hi-IN",
        ["pt"] = "pt-BR",
        ["sw"] = "sw-KE",
        ["ta"] = "ta-IN",
        ["ur"] = "ur-PK",
        ["zh"] = "zh-CN",
    };

    private static Dictionary<string, Voice> ChooseDefaults(Voice[] voices)
    {
        Dictionary<string, Voice> defaults = [];

        foreach (Voice voice in voices)
        {
            // A preview or a non-neural voice is not offered as a default: the
            // first is not guaranteed to stay, the second sounds worse. A name
            // carrying a colon belongs to an experimental series.
            if (voice.Status != "GA" || voice.VoiceType != "Neural" || voice.ShortName.Contains(':'))
            {
                continue;
            }

            string language = voice.Locale.Split('-')[0];

            if (!defaults.TryGetValue(language, out Voice? chosen))
            {
                defaults[language] = voice;
                continue;
            }

            if (Rank(voice) > Rank(chosen))
            {
                defaults[language] = voice;
            }
        }

        return defaults;
    }

    private static int Rank(Voice voice)
    {
        string language = voice.Locale.Split('-')[0];

        if (PreferredLocales.TryGetValue(language, out string? preferred))
        {
            return string.Equals(voice.Locale, preferred, StringComparison.OrdinalIgnoreCase) ? 2 : 0;
        }

        // Prefer the locale whose region repeats the language - ru-RU over
        // ru-KZ - which is the one a listener expects to hear.
        string[] parts = voice.Locale.Split('-');

        return parts.Length >= 2 && string.Equals(parts[0], parts[^1], StringComparison.OrdinalIgnoreCase) ? 1 : 0;
    }

    private static string Generate(Dictionary<string, Voice> defaults)
    {
        StringBuilder builder = new();

        builder.AppendLine("// <auto-generated>");
        builder.AppendLine("// Generated by tools/GLAZOV.M.TRANSLATE.Providers.Microsoft.Generator. Do not edit by hand.");
        builder.AppendLine("// </auto-generated>");
        builder.AppendLine();
        builder.AppendLine("using System.Collections.Immutable;");
        builder.AppendLine();
        builder.AppendLine("namespace GLAZOV.M.TRANSLATE.Providers.Microsoft.Internal;");
        builder.AppendLine();
        builder.AppendLine("internal static partial class MicrosoftVoices");
        builder.AppendLine("{");
        builder.AppendLine("    // Keyed by ISO 639-1 code. The value is the voice the speech endpoint");
        builder.AppendLine("    // speaks that language with unless the caller names another one.");
        builder.AppendLine("    private static readonly ImmutableDictionary<string, string> DefaultByIso6391 =");
        builder.AppendLine("        new Dictionary<string, string>(StringComparer.Ordinal)");
        builder.AppendLine("        {");

        foreach ((string language, Voice voice) in defaults.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            builder.AppendLine($"            [\"{language}\"] = \"{voice.ShortName}\", // {voice.LocaleName}");
        }

        builder.AppendLine("        }.ToImmutableDictionary();");
        builder.AppendLine("}");

        return builder.ToString();
    }
}
