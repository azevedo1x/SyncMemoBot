using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using SyncMemoBot.Core.Localization;
using SyncMemoBot.Core.RateLimiting;
using SyncMemoBot.Core.Reminders;
using SyncMemoBot.Core.Scheduling;
using SyncMemoBot.Core.Time;

namespace SyncMemoBot.Discord.Modules;

public sealed class ReminderCommandsModule(
    ITimeParsingService parser,
    IReminderScheduler scheduler,
    IReminderRateLimiter rateLimiter,
    ILocalizedMessages l10n,
    ILogger<ReminderCommandsModule> logger) : InteractionModuleBase<SocketInteractionContext>
{
    private const int MaxMessageLength = 1900;
    private const int MaxTimeLength = 100;

    private readonly ITimeParsingService _parser = parser;
    private readonly IReminderScheduler _scheduler = scheduler;
    private readonly IReminderRateLimiter _rateLimiter = rateLimiter;
    private readonly ILocalizedMessages _l10n = l10n;
    private readonly ILogger<ReminderCommandsModule> _logger = logger;

    [SlashCommand("remindme", "Schedule a private reminder")]
    public Task RemindMeAsync(
        [Summary("time", "When to remind (e.g., 'in 2 hours', 'amanhã às 15:00')"), MaxLength(MaxTimeLength)] string time,
        [Summary("message", "What to remember"), MaxLength(MaxMessageLength)] string message)
        => GuardAsync(async locale =>
        {
            if (await ResolveTimeOrRespondAsync(time, locale) is not { } whenUtc)
                return;

            if (!await TryAcquireOrRespondAsync(Context.User.Id, null, locale))
                return;

            await ScheduleAndConfirmAsync(
                new ReminderTarget.Direct(Context.User.Id, Context.User.Id),
                message,
                whenUtc,
                _l10n.PrivateConfirmation(locale, whenUtc));
        });

    [SlashCommand("remindsomeone", "Remind another user via DM")]
    public Task RemindSomeoneAsync(
        [Summary("user", "Who to remind")] IUser user,
        [Summary("time", "When to remind (e.g., 'in 2 hours', 'amanhã às 15:00')"), MaxLength(MaxTimeLength)] string time,
        [Summary("message", "What to remind them about"), MaxLength(MaxMessageLength)] string message)
        => GuardAsync(async locale =>
        {
            if (await ResolveTimeOrRespondAsync(time, locale) is not { } whenUtc)
                return;

            if (!await TryAcquireOrRespondAsync(Context.User.Id, user.Id, locale))
                return;

            await ScheduleAndConfirmAsync(
                new ReminderTarget.Direct(user.Id, Context.User.Id),
                message,
                whenUtc,
                _l10n.SomeoneConfirmation(locale, whenUtc, user.Id));
        });

    [SlashCommand("remindchannel", "Schedule a public reminder in a channel")]
    public Task RemindChannelAsync(
        [Summary("channel", "Target text channel"), ChannelTypes(ChannelType.Text)] ITextChannel channel,
        [Summary("time", "When to remind (e.g., 'in 2 hours', 'amanhã às 15:00')"), MaxLength(MaxTimeLength)] string time,
        [Summary("message", "What to remember"), MaxLength(MaxMessageLength)] string message)
        => GuardAsync(async locale =>
        {
            if (Context.User is not IGuildUser guildUser || !guildUser.GetPermissions(channel).SendMessages)
            {
                await FollowupAsync(_l10n.MissingChannelPermission(locale), ephemeral: true);
                return;
            }

            if (await ResolveTimeOrRespondAsync(time, locale) is not { } whenUtc)
                return;

            if (!await TryAcquireOrRespondAsync(Context.User.Id, null, locale))
                return;

            await ScheduleAndConfirmAsync(
                new ReminderTarget.Channel(channel.Id, Context.User.Id),
                message,
                whenUtc,
                _l10n.ChannelConfirmation(locale, whenUtc, channel.Id));
        });

    private async Task<DateTimeOffset?> ResolveTimeOrRespondAsync(string time, string? locale)
    {
        switch (_parser.Parse(time, locale))
        {
            case TimeParseResult.Success success:
                return success.ResolvedUtc;

            case TimeParseResult.Failure { Reason: TimeParseFailureReason.ResolvedInPast }:
                await FollowupAsync(_l10n.PastTimeError(locale), ephemeral: true);
                return null;

            default:
                await FollowupAsync(_l10n.TimeParseError(locale), ephemeral: true);
                return null;
        }
    }

    private async Task<bool> TryAcquireOrRespondAsync(ulong actorUserId, ulong? targetUserId, string? locale)
    {
        if (await _rateLimiter.TryAcquireAsync(actorUserId, targetUserId))
            return true;

        await FollowupAsync(_l10n.RateLimited(locale), ephemeral: true);
        return false;
    }

    private async Task ScheduleAndConfirmAsync(ReminderTarget target, string message, DateTimeOffset whenUtc, string confirmation)
    {
        _scheduler.Schedule(new ScheduledReminder(Guid.NewGuid(), target, message, whenUtc));
        await FollowupAsync(confirmation, ephemeral: true);
    }

    private async Task GuardAsync(Func<string?, Task> action)
    {
        var locale = Context.Interaction.UserLocale;
        try
        {
            await DeferAsync(ephemeral: true);
            await action(locale);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in reminder command");
            await FollowupAsync(_l10n.UnexpectedError(locale), ephemeral: true);
        }
    }
}
