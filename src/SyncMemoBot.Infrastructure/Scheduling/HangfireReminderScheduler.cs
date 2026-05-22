using Hangfire;
using SyncMemoBot.Core.Reminders;
using SyncMemoBot.Core.Scheduling;

namespace SyncMemoBot.Infrastructure.Scheduling;

public sealed class HangfireReminderScheduler : IReminderScheduler
{
    private readonly IBackgroundJobClient _client;

    public HangfireReminderScheduler(IBackgroundJobClient client)
        => _client = client;

    public string Schedule(ScheduledReminder reminder)
    {
        var delay = reminder.ScheduledAtUtc - DateTimeOffset.UtcNow;
        if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;

        return reminder.Target switch
        {
            ReminderTarget.Direct d => _client.Schedule<HangfireJobInvoker>(
                j => j.DispatchDirectAsync(d.UserId, reminder.Message, CancellationToken.None),
                delay),
            ReminderTarget.Channel c => _client.Schedule<HangfireJobInvoker>(
                j => j.DispatchChannelAsync(c.ChannelId, c.CreatedByUserId, reminder.Message, CancellationToken.None),
                delay),
            _ => throw new InvalidOperationException($"Unknown ReminderTarget: {reminder.Target.GetType().Name}")
        };
    }
}
