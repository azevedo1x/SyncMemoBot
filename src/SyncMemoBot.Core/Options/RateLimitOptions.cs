namespace SyncMemoBot.Core.Options;

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    public int BurstLimit { get; init; } = 5;

    public TimeSpan BurstWindow { get; init; } = TimeSpan.FromMinutes(1);

    public int DailyLimit { get; init; } = 50;

    public TimeSpan DailyWindow { get; init; } = TimeSpan.FromDays(1);

    public int PerTargetDailyLimit { get; init; } = 10;
}
