using System.Globalization;

namespace AxCrypt.App.Shared.Utility;

public static class CultureUtility
{
    private static readonly Dictionary<string, string> CultureMappings =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // English
            ["en"] = "en-US",

            // Mandarin Chinese
            ["zh"] = "zh-CN",

            // Spanish
            ["es"] = "es-ES",

            // French
            ["fr"] = "fr-FR",

            // German
            ["de"] = "de-DE",

            // Russian
            ["ru"] = "ru-RU",

            // Dutch
            ["nl"] = "nl-NL",

            // Swedish
            ["sv"] = "sv-SE",

            // Italian
            ["it"] = "it-IT",

            // Turkish
            ["tr"] = "tr-TR",

            // Korean
            ["ko"] = "ko-KR",

            // Portuguese
            ["pt"] = "pt-PT", // or pt-BR if Brazilian Portuguese

            // Polish
            ["pl"] = "pl-PL",

            // Arabic
            ["ar"] = "ar-SA"
        };

    public static CultureInfo GetSafeCulture(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return new CultureInfo("en-US");
        }

        // Convert neutral culture to specific culture
        if (CultureMappings.TryGetValue(cultureName, out var specificCulture))
        {
            cultureName = specificCulture;
        }

        try
        {
            return new CultureInfo(cultureName);
        }
        catch
        {
            return new CultureInfo("en-US");
        }
    }
}
