using SyncMemoBot.Core.Time;

namespace SyncMemoBot.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
