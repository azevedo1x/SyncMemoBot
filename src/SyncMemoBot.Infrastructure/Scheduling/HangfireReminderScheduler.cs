using Hangfire;
using SyncMemoBot.Core.Reminders;
using SyncMemoBot.Core.Scheduling;

namespace SyncMemoBot.Infrastructure.Scheduling;

public sealed class HangfireReminderScheduler(IBackgroundJobClient client) : IReminderScheduler
{
    private readonly IBackgroundJobClient _client = client;

    public string Schedule(ScheduledReminder reminder) => reminder.Target switch
    {
        ReminderTarget.Direct d => _client.Schedule<HangfireJobInvoker>(
            j => j.DispatchDirectAsync(d.UserId, d.CreatedByUserId, reminder.Message, reminder.Locale, null!, CancellationToken.None),
            reminder.ScheduledAtUtc),

        ReminderTarget.Channel c => _client.Schedule<HangfireJobInvoker>(
            j => j.DispatchChannelAsync(c.ChannelId, c.CreatedByUserId, reminder.Message, reminder.Locale, null!, CancellationToken.None),
            reminder.ScheduledAtUtc),

        _ => throw new InvalidOperationException($"Unknown ReminderTarget: {reminder.Target.GetType().Name}")
    };
}
