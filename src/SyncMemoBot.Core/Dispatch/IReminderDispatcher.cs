using SyncMemoBot.Core.Reminders;

namespace SyncMemoBot.Core.Dispatch;

public interface IReminderDispatcher
{
    Task DispatchAsync(ReminderTarget target, string message, CancellationToken ct = default);
}
