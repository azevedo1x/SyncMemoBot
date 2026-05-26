using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using SyncMemoBot.Core.Dispatch;
using SyncMemoBot.Core.Reminders;

namespace SyncMemoBot.Discord.Dispatch;

public sealed class DiscordReminderDispatcher : IReminderDispatcher
{
    private static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(30);

    // User-supplied reminder text must not be able to ping @everyone/@here or roles in a
    // channel; only the original author (a single user mention) is allowed through.
    private static readonly AllowedMentions ChannelMentions = new(AllowedMentionTypes.Users);

    private readonly DiscordSocketClient _client;
    private readonly DiscordReadinessSignal _readiness;
    private readonly ILogger<DiscordReminderDispatcher> _logger;

    public DiscordReminderDispatcher(
        DiscordSocketClient client,
        DiscordReadinessSignal readiness,
        ILogger<DiscordReminderDispatcher> logger)
    {
        _client = client;
        _readiness = readiness;
        _logger = logger;
    }

    public async Task DispatchAsync(ReminderTarget target, string message, CancellationToken ct = default)
    {
        // A job can fire before the gateway has connected (e.g. right after a restart, for a
        // reminder whose time already passed). Wait for the cache to be ready first; on timeout
        // the exception bubbles up so Hangfire retries instead of dropping the reminder.
        await _readiness.WaitUntilReadyAsync(ReadyTimeout, ct).ConfigureAwait(false);

        switch (target)
        {
            case ReminderTarget.Direct d:
                await DispatchDirectAsync(d, message).ConfigureAwait(false);
                break;
            case ReminderTarget.Channel c:
                await DispatchToChannelAsync(c, message).ConfigureAwait(false);
                break;
        }
    }

    private async Task DispatchDirectAsync(ReminderTarget.Direct target, string message)
    {
        var user = await _client.GetUserAsync(target.UserId).ConfigureAwait(false);
        if (user is null)
        {
            _logger.LogWarning("Reminder target user {UserId} not found", target.UserId);
            throw new InvalidOperationException($"Reminder target user {target.UserId} not found");
        }

        var dm = await user.CreateDMChannelAsync().ConfigureAwait(false);
        await dm.SendMessageAsync($"⏰ {message}").ConfigureAwait(false);
    }

    private async Task DispatchToChannelAsync(ReminderTarget.Channel target, string message)
    {
        if (_client.GetChannel(target.ChannelId) is not ITextChannel channel)
        {
            _logger.LogWarning("Reminder target channel {ChannelId} not found", target.ChannelId);
            throw new InvalidOperationException($"Reminder target channel {target.ChannelId} not found");
        }

        await channel.SendMessageAsync(
            $"⏰ <@{target.CreatedByUserId}> {message}",
            allowedMentions: ChannelMentions).ConfigureAwait(false);
    }
}
