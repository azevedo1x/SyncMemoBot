using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using SyncMemoBot.Core.Dispatch;
using SyncMemoBot.Core.Reminders;

namespace SyncMemoBot.Discord.Dispatch;

public sealed class DiscordReminderDispatcher : IReminderDispatcher
{
    private readonly DiscordSocketClient _client;
    private readonly ILogger<DiscordReminderDispatcher> _logger;

    public DiscordReminderDispatcher(DiscordSocketClient client, ILogger<DiscordReminderDispatcher> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task DispatchAsync(ReminderTarget target, string message, CancellationToken ct = default)
    {
        switch (target)
        {
            case ReminderTarget.Direct d:
                var user = await _client.GetUserAsync(d.UserId).ConfigureAwait(false);
                if (user is null)
                {
                    _logger.LogWarning("Reminder target user {UserId} not found; dropping reminder", d.UserId);
                    return;
                }
                var dm = await user.CreateDMChannelAsync().ConfigureAwait(false);
                await dm.SendMessageAsync($"⏰ {message}").ConfigureAwait(false);
                break;

            case ReminderTarget.Channel c:
                if (_client.GetChannel(c.ChannelId) is not ITextChannel channel)
                {
                    _logger.LogWarning("Reminder target channel {ChannelId} not found; dropping reminder", c.ChannelId);
                    return;
                }
                await channel.SendMessageAsync($"⏰ <@{c.CreatedByUserId}> {message}").ConfigureAwait(false);
                break;
        }
    }
}
