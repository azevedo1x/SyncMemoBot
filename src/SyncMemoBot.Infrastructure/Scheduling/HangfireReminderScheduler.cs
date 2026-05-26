using Hangfire;
using SyncMemoBot.Core.Reminders;
using SyncMemoBot.Core.Scheduling;

namespace SyncMemoBot.Infrastructure.Scheduling;

public sealed class HangfireReminderScheduler : IReminderScheduler
{
    private readonly IBackgroundJobClient _client;

    public HangfireReminderScheduler(IBackgroundJobClient client)
        => _client = client;

    // Hangfire's DateTimeOffset overload enqueues immediately if the instant is already
    // past, so no manual delay/clamp is needed here.
    public string Schedule(ScheduledReminder reminder) => reminder.Target switch
    {
        ReminderTarget.Direct d => _client.Schedule<HangfireJobInvoker>(
            j => j.DispatchDirectAsync(d.UserId, reminder.Message, CancellationToken.None),
            reminder.ScheduledAtUtc),
        ReminderTarget.Channel c => _client.Schedule<HangfireJobInvoker>(
            j => j.DispatchChannelAsync(c.ChannelId, c.CreatedByUserId, reminder.Message, CancellationToken.None),
            reminder.ScheduledAtUtc),
        _ => throw new InvalidOperationException($"Unknown ReminderTarget: {reminder.Target.GetType().Name}")
    };
}
