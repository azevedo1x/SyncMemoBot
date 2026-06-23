using Discord;
using Discord.Net;
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

        try
        {
            var send = target switch
            {
                ReminderTarget.Direct d  => DispatchDirectAsync(d, message),
                ReminderTarget.Channel c => DispatchToChannelAsync(c, message),
                _ => throw new InvalidOperationException($"Unknown ReminderTarget: {target.GetType().Name}")
            };

            await send.ConfigureAwait(false);
        }
        catch (HttpException ex)
        {
            throw new ReminderDeliveryException(MapReason(ex), ex);
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
            throw new ReminderDeliveryException(DeliveryFailureReason.TargetUnreachable);
        }

        var dm = await user.CreateDMChannelAsync().ConfigureAwait(false);
        await dm.SendMessageAsync(content).ConfigureAwait(false);
    }

    private async Task DispatchToChannelAsync(ReminderTarget.Channel target, string message)
    {
        if (_client.GetChannel(target.ChannelId) is not ITextChannel channel)
        {
            _logger.LogWarning("Reminder target channel {ChannelId} not found", target.ChannelId);
            throw new ReminderDeliveryException(DeliveryFailureReason.ChannelNotFound);
        }

        await channel.SendMessageAsync($"⏰ <@{target.CreatedByUserId}> {message}").ConfigureAwait(false);
    }

    private static DeliveryFailureReason MapReason(HttpException ex) => (int?)ex.DiscordCode switch
    {
        50007 => DeliveryFailureReason.TargetUnreachable,
        50278 => DeliveryFailureReason.TargetUnreachable,
        50013 => DeliveryFailureReason.MissingChannelPermission,
        50001 => DeliveryFailureReason.MissingChannelPermission,
        10003 => DeliveryFailureReason.ChannelNotFound,
        _ => DeliveryFailureReason.Unknown
    };
}
