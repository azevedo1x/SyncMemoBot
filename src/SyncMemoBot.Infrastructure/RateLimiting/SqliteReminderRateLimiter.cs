using Microsoft.Extensions.Options;
using SQLite;
using SyncMemoBot.Core.Options;
using SyncMemoBot.Core.RateLimiting;
using SyncMemoBot.Core.Time;

namespace SyncMemoBot.Infrastructure.RateLimiting;

public sealed class SqliteReminderRateLimiter(string databasePath, IClock clock, IOptions<RateLimitOptions> options) : IReminderRateLimiter
{
    private readonly SQLiteAsyncConnection _db = new(databasePath);
    private readonly IClock _clock = clock;
    private readonly RateLimitOptions _options = options.Value;
    private Task? _initialization;

    public async Task<bool> TryAcquireAsync(ulong actorUserId, ulong? targetUserId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync().ConfigureAwait(false);

        var nowTicks = _clock.UtcNow.UtcDateTime.Ticks;
        var actor = unchecked((long)actorUserId);

        var longestWindow = _options.BurstWindow > _options.DailyWindow
            ? _options.BurstWindow
            : _options.DailyWindow;

        await _db.ExecuteAsync(
            "DELETE FROM RateLimitEntry WHERE CreatedAtTicks < ?",
            nowTicks - longestWindow.Ticks).ConfigureAwait(false);

        if (await CountSinceAsync(actor, nowTicks - _options.BurstWindow.Ticks).ConfigureAwait(false) >= _options.BurstLimit)
            return false;

        if (await CountSinceAsync(actor, nowTicks - _options.DailyWindow.Ticks).ConfigureAwait(false) >= _options.DailyLimit)
            return false;

        if (targetUserId is { } rawTarget)
        {
            var target = unchecked((long)rawTarget);
            var cutoff = nowTicks - _options.DailyWindow.Ticks;

            var perTarget = await _db.Table<RateLimitEntry>()
                .Where(e => e.ActorUserId == actor && e.TargetUserId == target && e.CreatedAtTicks >= cutoff)
                .CountAsync().ConfigureAwait(false);

            if (perTarget >= _options.PerTargetDailyLimit)
                return false;
        }

        await _db.InsertAsync(new RateLimitEntry
        {
            ActorUserId = actor,
            TargetUserId = unchecked((long)(targetUserId ?? 0)),
            CreatedAtTicks = nowTicks
        }).ConfigureAwait(false);

        return true;
    }

    private Task<int> CountSinceAsync(long actor, long cutoffTicks) =>
        _db.Table<RateLimitEntry>()
            .Where(e => e.ActorUserId == actor && e.CreatedAtTicks >= cutoffTicks)
            .CountAsync();

    private Task EnsureInitializedAsync() =>
        _initialization ??= _db.CreateTableAsync<RateLimitEntry>();
}
