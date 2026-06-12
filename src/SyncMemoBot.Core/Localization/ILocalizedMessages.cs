namespace SyncMemoBot.Core.Localization;

public interface ILocalizedMessages
{
    string TimeParseError(string? userLocale);

    string PastTimeError(string? userLocale);

    string PrivateConfirmation(string? userLocale, DateTimeOffset whenUtc);

    string SomeoneConfirmation(string? userLocale, DateTimeOffset whenUtc, ulong targetUserId);

    string ChannelConfirmation(string? userLocale, DateTimeOffset whenUtc, ulong channelId);

    string MissingChannelPermission(string? userLocale);

    string UnexpectedError(string? userLocale);

    string RateLimited(string? userLocale);
}
