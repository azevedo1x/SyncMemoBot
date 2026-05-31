using Discord;
using Discord.WebSocket;
using SyncMemoBot.Core.Dispatch;
using SyncMemoBot.Core.Reminders;

namespace SyncMemoBot.Discord.Dispatch;

public sealed class DiscordReminderDispatcher(
    DiscordSocketClient client,
    DiscordReadinessSignal readiness,
    ILogger<DiscordReminderDispatcher> logger) : IReminderDispatcher
{
    private static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(30);

    private readonly DiscordSocketClient _client = client;
    private readonly DiscordReadinessSignal _readiness = readiness;
    private readonly ILogger<DiscordReminderDispatcher> _logger = logger;

    public async Task DispatchAsync(ReminderTarget target, string message, CancellationToken ct = default)
    {
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

    private Task DispatchDirectAsync(ReminderTarget.Direct target, string message)
    {
        var content = target.UserId == target.CreatedByUserId
            ? $"⏰ {message}"
            : $"⏰ <@{target.CreatedByUserId}>: {message}";

        return SendDmAsync(target.UserId, content);
    }

    private async Task SendDmAsync(ulong userId, string content)
    {
        var user = await _client.GetUserAsync(userId).ConfigureAwait(false);
        if (user is null)
        {
            _logger.LogWarning("Reminder target user {UserId} not found", userId);
            throw new InvalidOperationException($"Reminder target user {userId} not found");
        }

        var dm = await user.CreateDMChannelAsync().ConfigureAwait(false);
        await dm.SendMessageAsync(content).ConfigureAwait(false);
    }

    private async Task DispatchToChannelAsync(ReminderTarget.Channel target, string message)
    {
        if (_client.GetChannel(target.ChannelId) is not ITextChannel channel)
        {
            _logger.LogWarning("Reminder target channel {ChannelId} not found", target.ChannelId);
            throw new InvalidOperationException($"Reminder target channel {target.ChannelId} not found");
        }

        await channel.SendMessageAsync($"⏰ <@{target.CreatedByUserId}> {message}").ConfigureAwait(false);
    }
}
