namespace SyncMemoBot.Core.Time;

public abstract record TimeParseResult
{
    private TimeParseResult() { }

    public sealed record Success(DateTimeOffset ResolvedUtc, string UsedCulture) : TimeParseResult;

    public sealed record Failure(TimeParseFailureReason Reason) : TimeParseResult;
}

public enum TimeParseFailureReason
{
    NoDateTimeFound,
    ResolvedInPast
}
