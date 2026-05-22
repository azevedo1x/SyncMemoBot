using Discord;
using Discord.Interactions;
using SyncMemoBot.Core.Localization;
using SyncMemoBot.Core.Reminders;
using SyncMemoBot.Core.Scheduling;
using SyncMemoBot.Core.Time;

namespace SyncMemoBot.Discord.Modules;

public sealed class ReminderCommandsModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ITimeParsingService _parser;
    private readonly IReminderScheduler _scheduler;
    private readonly ILocalizedMessages _l10n;

    public ReminderCommandsModule(ITimeParsingService parser, IReminderScheduler scheduler, ILocalizedMessages l10n)
    {
        _parser = parser;
        _scheduler = scheduler;
        _l10n = l10n;
    }

    [SlashCommand("remind", "Schedule a private reminder")]
    public async Task RemindAsync(
        [Summary("time", "When to remind (e.g., 'in 2 hours', 'amanhã às 15:00')")] string time,
        [Summary("message", "What to remember")] string message)
    {
        var locale = Context.Interaction.UserLocale;

        if (_parser.Parse(time, locale) is not TimeParseResult.Success ok)
        {
            await RespondAsync(_l10n.TimeParseError(locale), ephemeral: true);
            return;
        }

        var reminder = new ScheduledReminder(
            Guid.NewGuid(),
            new ReminderTarget.Direct(Context.User.Id),
            message,
            ok.ResolvedUtc);

        _scheduler.Schedule(reminder);

        await RespondAsync(_l10n.PrivateConfirmation(locale, ok.ResolvedUtc), ephemeral: true);
    }

    [SlashCommand("remindchannel", "Schedule a public reminder in a channel")]
    public async Task RemindChannelAsync(
        [Summary("channel", "Target text channel"), ChannelTypes(ChannelType.Text)] ITextChannel channel,
        [Summary("time", "When to remind (e.g., 'in 2 hours', 'amanhã às 15:00')")] string time,
        [Summary("message", "What to remember")] string message)
    {
        var locale = Context.Interaction.UserLocale;

        if (Context.User is not IGuildUser guildUser ||
            !guildUser.GetPermissions(channel).SendMessages)
        {
            await RespondAsync(_l10n.MissingChannelPermission(locale), ephemeral: true);
            return;
        }

        if (_parser.Parse(time, locale) is not TimeParseResult.Success ok)
        {
            await RespondAsync(_l10n.TimeParseError(locale), ephemeral: true);
            return;
        }

        var reminder = new ScheduledReminder(
            Guid.NewGuid(),
            new ReminderTarget.Channel(channel.Id, Context.User.Id),
            message,
            ok.ResolvedUtc);

        _scheduler.Schedule(reminder);

        await RespondAsync(_l10n.ChannelConfirmation(locale, ok.ResolvedUtc, channel.Id), ephemeral: true);
    }
}
