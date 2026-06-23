namespace SyncMemoBot.Core.Dispatch;

public enum DeliveryFailureReason
{
    Unknown,
    TargetUnreachable,
    MissingChannelPermission,
    ChannelNotFound
}
