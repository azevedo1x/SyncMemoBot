namespace SyncMemoBot.Core.Localization;

public static class LanguageDetection
{
    /// <summary>
    /// Single source of truth for mapping a raw locale (e.g. Discord's UserLocale)
    /// to a supported language. Only PT is detected explicitly; everything else is English.
    /// </summary>
    /// <param name="locale"></param>
    /// <returns></returns>
    public static SupportedLanguage FromLocale(string? locale) =>
        !string.IsNullOrEmpty(locale) && locale.TrimStart().StartsWith("pt", StringComparison.OrdinalIgnoreCase)
            ? SupportedLanguage.Portuguese
            : SupportedLanguage.English;
}
