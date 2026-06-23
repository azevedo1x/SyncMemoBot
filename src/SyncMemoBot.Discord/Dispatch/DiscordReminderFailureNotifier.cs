using Discord.WebSocket;
using SyncMemoBot.Core.Dispatch;
using SyncMemoBot.Core.Localization;

namespace SyncMemoBot.Discord.Dispatch;

public sealed class DiscordReminderFailureNotifier(
    DiscordSocketClient client,
    ILocalizedMessages l10n,
    ILogger<DiscordReminderFailureNotifier> logger) : IReminderFailureNotifier
{
    private readonly DiscordSocketClient _client = client;
    private readonly ILocalizedMessages _l10n = l10n;
    private readonly ILogger<DiscordReminderFailureNotifier> _logger = logger;

    public async Task NotifyDeliveryFailureAsync(ulong createdByUserId, string message, DeliveryFailureReason reason, string? locale, CancellationToken ct = default)
    {
        try
        {
            var user = await _client.GetUserAsync(createdByUserId);
            if (user is null)
            {
                _logger.LogWarning("Could not notify creator {UserId}: user not found", createdByUserId);
                return;
            }

            var dm = await user.CreateDMChannelAsync();
            await dm.SendMessageAsync(_l10n.DeliveryFailure(reason, message, locale));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify creator {UserId} about delivery failure", createdByUserId);
        }
    }
}
