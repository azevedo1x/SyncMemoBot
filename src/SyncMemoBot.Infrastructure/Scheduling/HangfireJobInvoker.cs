using Hangfire;
using Hangfire.Server;
using SyncMemoBot.Core.Dispatch;
using SyncMemoBot.Core.Reminders;

namespace SyncMemoBot.Infrastructure.Scheduling;

[AutomaticRetry(Attempts = MaxAttempts, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
public sealed class HangfireJobInvoker(IReminderDispatcher dispatcher, IReminderFailureNotifier notifier)
{
    public const int MaxAttempts = 3;

    private readonly IReminderDispatcher _dispatcher = dispatcher;
    private readonly IReminderFailureNotifier _notifier = notifier;

    public Task DispatchDirectAsync(ulong userId, ulong createdByUserId, string message, string? locale, PerformContext context, CancellationToken ct = default)
        => RunAsync(new ReminderTarget.Direct(userId, createdByUserId), createdByUserId, message, locale, context, ct);

    public Task DispatchChannelAsync(ulong channelId, ulong createdByUserId, string message, string? locale, PerformContext context, CancellationToken ct = default)
        => RunAsync(new ReminderTarget.Channel(channelId, createdByUserId), createdByUserId, message, locale, context, ct);

    private async Task RunAsync(ReminderTarget target, ulong createdByUserId, string message, string? locale, PerformContext context, CancellationToken ct)
    {
        try
        {
            await _dispatcher.DispatchAsync(target, message, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (context.GetJobParameter<int>("RetryCount") >= MaxAttempts)
            {
                var reason = ex is ReminderDeliveryException delivery
                    ? delivery.Reason
                    : DeliveryFailureReason.Unknown;

                await _notifier.NotifyDeliveryFailureAsync(createdByUserId, message, reason, locale, ct).ConfigureAwait(false);
            }
            throw;
        }
    }
}
