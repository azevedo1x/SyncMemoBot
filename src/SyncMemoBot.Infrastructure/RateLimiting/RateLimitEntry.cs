using SQLite;

namespace SyncMemoBot.Infrastructure.RateLimiting;

public sealed class RateLimitEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public long ActorUserId { get; set; }

    public long TargetUserId { get; set; }

    [Indexed]
    public long CreatedAtTicks { get; set; }
}
