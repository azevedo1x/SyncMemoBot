namespace SyncMemoBot.Core.Dispatch;

public sealed class ReminderDeliveryException(DeliveryFailureReason reason, Exception? innerException = null)
    : Exception($"Reminder delivery failed: {reason}", innerException)
{
    public DeliveryFailureReason Reason { get; } = reason;
}
