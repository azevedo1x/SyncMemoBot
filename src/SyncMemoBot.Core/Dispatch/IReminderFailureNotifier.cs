namespace SyncMemoBot.Core.Dispatch;

public interface IReminderFailureNotifier
{
    Task NotifyDeliveryFailureAsync(ulong createdByUserId, string message, DeliveryFailureReason reason, string? locale, CancellationToken ct = default);
}
