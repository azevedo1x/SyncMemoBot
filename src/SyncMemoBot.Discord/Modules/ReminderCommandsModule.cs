using Discord;
using Discord.Interactions;
using SyncMemoBot.Core.Localization;
using SyncMemoBot.Core.Reminders;
using SyncMemoBot.Core.Scheduling;
using SyncMemoBot.Core.Time;

namespace SyncMemoBot.Discord.Modules;

public sealed class ReminderCommandsModule(
    ITimeParsingService parser,
    IReminderScheduler scheduler,
    ILocalizedMessages l10n) : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ITimeParsingService _parser = parser;
    private readonly IReminderScheduler _scheduler = scheduler;
    private readonly ILocalizedMessages _l10n = l10n;

    [SlashCommand("remindme", "Schedule a private reminder")]
    public async Task RemindMeAsync(
        [Summary("time", "When to remind (e.g., 'in 2 hours', 'amanhã às 15:00')")] string time,
        [Summary("message", "What to remember")] string message)
    {
        var locale = Context.Interaction.UserLocale;
        if (await ResolveTimeOrRespondAsync(time, locale) is not { } whenUtc)
            return;

        await ScheduleAndConfirmAsync(
            new ReminderTarget.Direct(Context.User.Id, Context.User.Id),
            message,
            whenUtc,
            _l10n.PrivateConfirmation(locale, whenUtc));
    }

    [SlashCommand("remindsomeone", "Remind another user via DM")]
    public async Task RemindSomeoneAsync(
        [Summary("user", "Who to remind")] IUser user,
        [Summary("time", "When to remind (e.g., 'in 2 hours', 'amanhã às 15:00')")] string time,
        [Summary("message", "What to remind them about")] string message)
    {
        var locale = Context.Interaction.UserLocale;
        if (await ResolveTimeOrRespondAsync(time, locale) is not { } whenUtc)
            return;

        await ScheduleAndConfirmAsync(
            new ReminderTarget.Direct(user.Id, Context.User.Id),
            message,
            whenUtc,
            _l10n.SomeoneConfirmation(locale, whenUtc, user.Id));
    }

    [SlashCommand("remindchannel", "Schedule a public reminder in a channel")]
    public async Task RemindChannelAsync(
        [Summary("channel", "Target text channel"), ChannelTypes(ChannelType.Text)] ITextChannel channel,
        [Summary("time", "When to remind (e.g., 'in 2 hours', 'amanhã às 15:00')")] string time,
        [Summary("message", "What to remember")] string message)
    {
        var locale = Context.Interaction.UserLocale;

        if (Context.User is not IGuildUser guildUser || !guildUser.GetPermissions(channel).SendMessages)
        {
            await RespondAsync(_l10n.MissingChannelPermission(locale), ephemeral: true);
            return;
        }

        if (await ResolveTimeOrRespondAsync(time, locale) is not { } whenUtc)
            return;

        await ScheduleAndConfirmAsync(
            new ReminderTarget.Channel(channel.Id, Context.User.Id),
            message,
            whenUtc,
            _l10n.ChannelConfirmation(locale, whenUtc, channel.Id));
    }

    /// <summary>
    /// Returns the resolved instant, or null after already replying to the user with the
    /// appropriate error (couldn't parse vs. resolved in the past).
    /// </summary>
    /// <param name="time"></param>
    /// <param name="locale"></param>
    /// <returns></returns>

    private async Task<DateTimeOffset?> ResolveTimeOrRespondAsync(string time, string? locale)
    {
        switch (_parser.Parse(time, locale))
        {
            case TimeParseResult.Success success:
                return success.ResolvedUtc;

            case TimeParseResult.Failure { Reason: TimeParseFailureReason.ResolvedInPast }:
                await RespondAsync(_l10n.PastTimeError(locale), ephemeral: true);
                return null;

            default:
                await RespondAsync(_l10n.TimeParseError(locale), ephemeral: true);
                return null;
        }
    }

    private async Task ScheduleAndConfirmAsync(ReminderTarget target, string message, DateTimeOffset whenUtc, string confirmation)
    {
        _scheduler.Schedule(new ScheduledReminder(Guid.NewGuid(), target, message, whenUtc));
        await RespondAsync(confirmation, ephemeral: true);
    }
}
