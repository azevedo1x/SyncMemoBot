namespace SyncMemoBot.Core.Reminders;

public abstract record ReminderTarget
{
    private ReminderTarget() { }

    public sealed record Direct(ulong UserId, ulong CreatedByUserId) : ReminderTarget;

    public sealed record Channel(ulong ChannelId, ulong CreatedByUserId) : ReminderTarget;
}
