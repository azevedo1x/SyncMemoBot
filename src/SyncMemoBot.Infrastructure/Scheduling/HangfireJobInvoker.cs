using SyncMemoBot.Core.Dispatch;
using SyncMemoBot.Core.Reminders;

namespace SyncMemoBot.Infrastructure.Scheduling;

public sealed class HangfireJobInvoker
{
    private readonly IReminderDispatcher _dispatcher;

    public HangfireJobInvoker(IReminderDispatcher dispatcher)
        => _dispatcher = dispatcher;

    public Task DispatchDirectAsync(ulong userId, string message, CancellationToken ct = default)
        => _dispatcher.DispatchAsync(new ReminderTarget.Direct(userId), message, ct);

    public Task DispatchChannelAsync(ulong channelId, ulong createdByUserId, string message, CancellationToken ct = default)
        => _dispatcher.DispatchAsync(new ReminderTarget.Channel(channelId, createdByUserId), message, ct);
}
