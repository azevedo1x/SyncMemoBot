namespace SyncMemoBot.Core.RateLimiting;

public interface IReminderRateLimiter
{
    Task<bool> TryAcquireAsync(ulong actorUserId, ulong? targetUserId, CancellationToken ct = default);
}
