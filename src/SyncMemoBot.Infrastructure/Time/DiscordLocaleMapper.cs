using Microsoft.Recognizers.Text;

namespace SyncMemoBot.Infrastructure.Time;

internal static class DiscordLocaleMapper
{
    public static string ToRecognizersCulture(string? discordLocale)
    {
        if (string.IsNullOrEmpty(discordLocale)) return Culture.English;
        if (discordLocale.StartsWith("pt", StringComparison.OrdinalIgnoreCase)) return Culture.Portuguese;
        if (discordLocale.StartsWith("es", StringComparison.OrdinalIgnoreCase)) return Culture.Spanish;
        return Culture.English;
    }
}
