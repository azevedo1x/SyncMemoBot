namespace SyncMemoBot.Core.Localization;

public interface ILocalizedMessages
{
    string TimeParseError(string? userLocale);

    string PastTimeError(string? userLocale);

    string PrivateConfirmation(string? userLocale, DateTimeOffset whenUtc);

    string ChannelConfirmation(string? userLocale, DateTimeOffset whenUtc, ulong channelId);

    string MissingChannelPermission(string? userLocale);
}
