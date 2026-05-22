using System.Globalization;
using SyncMemoBot.Core.Localization;
using SyncMemoBot.Core.Time;

namespace SyncMemoBot.Infrastructure.Localization;

public sealed class LocalizedMessages : ILocalizedMessages
{
    private readonly IReminderTimeZone _timeZone;

    public LocalizedMessages(IReminderTimeZone timeZone)
        => _timeZone = timeZone;

    public string TimeParseError(string? userLocale) => NormalizeLanguage(userLocale) switch
    {
        Lang.Portuguese => "Não consegui entender o horário. Tente: 'em 2 horas', 'amanhã às 15:00' ou 'amanhã às 3 da tarde'.",
        Lang.Spanish    => "No pude entender la hora. Prueba: 'en 2 horas' o 'mañana a las 15:00'.",
        _               => "Couldn't parse that time. Try: 'in 2 hours' or 'tomorrow at 3pm'."
    };

    public string PrivateConfirmation(string? userLocale, DateTimeOffset whenUtc)
    {
        var (lang, culture) = ResolveLanguageAndCulture(userLocale);
        var formatted = FormatInZone(whenUtc, culture);
        return lang switch
        {
            Lang.Portuguese => $"Lembrete agendado para **{formatted}**. Vou te avisar por DM.",
            Lang.Spanish    => $"Recordatorio programado para **{formatted}**. Te avisaré por DM.",
            _               => $"Reminder scheduled for **{formatted}**. I'll DM you."
        };
    }

    public string ChannelConfirmation(string? userLocale, DateTimeOffset whenUtc, ulong channelId)
    {
        var (lang, culture) = ResolveLanguageAndCulture(userLocale);
        var formatted = FormatInZone(whenUtc, culture);
        return lang switch
        {
            Lang.Portuguese => $"Lembrete agendado em <#{channelId}> para **{formatted}**.",
            Lang.Spanish    => $"Recordatorio programado en <#{channelId}> para **{formatted}**.",
            _               => $"Reminder scheduled in <#{channelId}> for **{formatted}**."
        };
    }

    public string MissingChannelPermission(string? userLocale) => NormalizeLanguage(userLocale) switch
    {
        Lang.Portuguese => "Você não tem permissão de enviar mensagens nesse canal.",
        Lang.Spanish    => "No tienes permiso para enviar mensajes en ese canal.",
        _               => "You don't have permission to send messages in that channel."
    };

    private string FormatInZone(DateTimeOffset whenUtc, CultureInfo culture)
    {
        var local = TimeZoneInfo.ConvertTime(whenUtc, _timeZone.Zone);
        return local.ToString("g", culture);
    }

    private static (Lang, CultureInfo) ResolveLanguageAndCulture(string? userLocale)
    {
        var lang = NormalizeLanguage(userLocale);
        var culture = lang switch
        {
            Lang.Portuguese => CultureInfo.GetCultureInfo("pt-BR"),
            Lang.Spanish    => CultureInfo.GetCultureInfo("es-ES"),
            _               => CultureInfo.GetCultureInfo("en-US")
        };
        return (lang, culture);
    }

    private static Lang NormalizeLanguage(string? userLocale)
    {
        if (string.IsNullOrEmpty(userLocale)) return Lang.English;
        if (userLocale.StartsWith("pt", StringComparison.OrdinalIgnoreCase)) return Lang.Portuguese;
        if (userLocale.StartsWith("es", StringComparison.OrdinalIgnoreCase)) return Lang.Spanish;
        return Lang.English;
    }

    private enum Lang { English, Portuguese, Spanish }
}
