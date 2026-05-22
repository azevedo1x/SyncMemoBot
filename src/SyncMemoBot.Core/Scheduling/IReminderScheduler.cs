using SyncMemoBot.Core.Reminders;

namespace SyncMemoBot.Core.Scheduling;

public interface IReminderScheduler
{
    string Schedule(ScheduledReminder reminder);
}
