namespace SyncMemoBot.Core.Time;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
