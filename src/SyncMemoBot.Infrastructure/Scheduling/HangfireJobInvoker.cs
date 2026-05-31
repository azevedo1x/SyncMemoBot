using SyncMemoBot.Core.Dispatch;
using SyncMemoBot.Core.Reminders;

namespace SyncMemoBot.Infrastructure.Scheduling;

public sealed class HangfireJobInvoker(IReminderDispatcher dispatcher)
{
    private readonly IReminderDispatcher _dispatcher = dispatcher;

    public Task DispatchDirectAsync(ulong userId, ulong createdByUserId, string message, CancellationToken ct = default)
        => _dispatcher.DispatchAsync(new ReminderTarget.Direct(userId, createdByUserId), message, ct);

    public Task DispatchChannelAsync(ulong channelId, ulong createdByUserId, string message, CancellationToken ct = default)
        => _dispatcher.DispatchAsync(new ReminderTarget.Channel(channelId, createdByUserId), message, ct);
}
