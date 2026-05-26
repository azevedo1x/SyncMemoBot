namespace SyncMemoBot.Discord;

/// <summary>
/// Shared gate between the host (which signals once the gateway is Ready) and the reminder
/// dispatcher (which waits before touching the client cache). Prevents reminders that fire
/// during the startup window — before the gateway connects — from seeing an empty cache.
/// </summary>
public sealed class DiscordReadinessSignal
{
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void SignalReady() => _ready.TrySetResult();

    public Task WaitUntilReadyAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        => _ready.Task.WaitAsync(timeout, cancellationToken);
}
