using System.Globalization;
using SyncMemoBot.Core.Dispatch;
using SyncMemoBot.Core.Localization;
using SyncMemoBot.Core.Time;

namespace SyncMemoBot.Infrastructure.Localization;

public sealed class LocalizedMessages(IReminderTimeZone timeZone) : ILocalizedMessages
{
    private readonly IReminderTimeZone _timeZone = timeZone;

    public string TimeParseError(string? userLocale) => LanguageDetection.FromLocale(userLocale) switch
    {
        SupportedLanguage.Portuguese => "Não consegui entender o horário. Tente: 'em 2 horas', 'amanhã às 15:00', '15:00' ou 'amanhã às 3 da tarde'.",
        _                            => "Couldn't parse that time. Try: 'in 2 hours', 'tomorrow at 3pm', or '15:00'."
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

    public string SomeoneConfirmation(string? userLocale, DateTimeOffset whenUtc, ulong targetUserId)
    {
        var language = LanguageDetection.FromLocale(userLocale);
        var formatted = FormatInZone(whenUtc, language);
        return language switch
        {
            SupportedLanguage.Portuguese => $"Lembrete agendado para <@{targetUserId}> em **{formatted}**. Vou avisar por DM.",
            _                            => $"Reminder scheduled for <@{targetUserId}> at **{formatted}**. I'll DM them."
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

    public string BotMissingChannelPermission(string? userLocale) => LanguageDetection.FromLocale(userLocale) switch
    {
        SupportedLanguage.Portuguese => "Eu não tenho permissão de enviar mensagens nesse canal. Peça a um admin para liberar.",
        _                            => "I don't have permission to send messages in that channel. Ask an admin to grant it."
    };

    public string UnexpectedError(string? userLocale) => LanguageDetection.FromLocale(userLocale) switch
    {
        SupportedLanguage.Portuguese => "Algo deu errado ao agendar seu lembrete. Tente novamente.",
        _                            => "Something went wrong scheduling your reminder. Please try again."
    };

    public string RateLimited(string? userLocale) => LanguageDetection.FromLocale(userLocale) switch
    {
        SupportedLanguage.Portuguese => "Você está agendando lembretes rápido demais. Espere um pouco e tente de novo.",
        _                            => "You're scheduling reminders too fast. Wait a bit and try again."
    };

    public string TargetUnreachable(string? userLocale) => LanguageDetection.FromLocale(userLocale) switch
    {
        SupportedLanguage.Portuguese => "O destinatário não aceita mensagens diretas ou não compartilha um servidor comigo.",
        _                            => "The recipient doesn't accept DMs or shares no server with me."
    };

    public string ChannelNotFound(string? userLocale) => LanguageDetection.FromLocale(userLocale) switch
    {
        SupportedLanguage.Portuguese => "O canal de destino não foi encontrado.",
        _                            => "The target channel was not found."
    };

    public string DeliveryFailure(DeliveryFailureReason reason, string reminderMessage, string? userLocale)
    {
        var preview = Preview(reminderMessage);
        var reasonMessage = GetReasonMessage(reason, userLocale);

        return LanguageDetection.FromLocale(userLocale) switch
        {
            SupportedLanguage.Portuguese => $"⏰ Não consegui entregar seu lembrete **{preview}**: {reasonMessage}",
            _                            => $"⏰ Couldn't deliver your reminder **{preview}**: {reasonMessage}"
        };
    }

    private string GetReasonMessage(DeliveryFailureReason reason, string? userLocale) => reason switch
    {
        DeliveryFailureReason.MissingChannelPermission => BotMissingChannelPermission(userLocale),
        DeliveryFailureReason.TargetUnreachable        => TargetUnreachable(userLocale),
        DeliveryFailureReason.ChannelNotFound          => ChannelNotFound(userLocale),
        _                                              => UnexpectedError(userLocale)
    };

    private static string Preview(string text)
    {
        if (text.Length <= 200) return text;
        var end = char.IsHighSurrogate(text[199]) ? 199 : 200;
        return text[..end] + "…";
    }

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
