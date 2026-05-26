using System.Globalization;
using SyncMemoBot.Core.Localization;
using SyncMemoBot.Core.Time;

namespace SyncMemoBot.Infrastructure.Localization;

public sealed class LocalizedMessages(IReminderTimeZone timeZone) : ILocalizedMessages
{
    private readonly IReminderTimeZone _timeZone = timeZone;

    public string TimeParseError(string? userLocale) => LanguageDetection.FromLocale(userLocale) switch
    {
        SupportedLanguage.Portuguese => "Não consegui entender o horário. Tente: 'em 2 horas', 'amanhã às 15:00' ou 'amanhã às 3 da tarde'.",
        _                            => "Couldn't parse that time. Try: 'in 2 hours' or 'tomorrow at 3pm'."
    };

    public string PastTimeError(string? userLocale) => LanguageDetection.FromLocale(userLocale) switch
    {
        SupportedLanguage.Portuguese => "Esse horário já passou. Tente um horário no futuro, como 'amanhã às 15:00'.",
        _                            => "That time is already in the past. Try a future time, like 'tomorrow at 3pm'."
    };

    public string PrivateConfirmation(string? userLocale, DateTimeOffset whenUtc)
    {
        var language = LanguageDetection.FromLocale(userLocale);
        var formatted = FormatInZone(whenUtc, language);
        return language switch
        {
            SupportedLanguage.Portuguese => $"Lembrete agendado para **{formatted}**. Vou te avisar por DM.",
            _                            => $"Reminder scheduled for **{formatted}**. I'll DM you."
        };
    }

    public string ChannelConfirmation(string? userLocale, DateTimeOffset whenUtc, ulong channelId)
    {
        var language = LanguageDetection.FromLocale(userLocale);
        var formatted = FormatInZone(whenUtc, language);
        return language switch
        {
            SupportedLanguage.Portuguese => $"Lembrete agendado em <#{channelId}> para **{formatted}**.",
            _                            => $"Reminder scheduled in <#{channelId}> for **{formatted}**."
        };
    }

    public string MissingChannelPermission(string? userLocale) => LanguageDetection.FromLocale(userLocale) switch
    {
        SupportedLanguage.Portuguese => "Você não tem permissão de enviar mensagens nesse canal.",
        _                            => "You don't have permission to send messages in that channel."
    };

    private string FormatInZone(DateTimeOffset whenUtc, SupportedLanguage language)
    {
        var local = TimeZoneInfo.ConvertTime(whenUtc, _timeZone.Zone);
        return local.ToString("g", CultureFor(language));
    }

    private static CultureInfo CultureFor(SupportedLanguage language) => language switch
    {
        SupportedLanguage.Portuguese => CultureInfo.GetCultureInfo("pt-BR"),
        _                            => CultureInfo.GetCultureInfo("en-US")
    };
}
