namespace SyncMemoBot.Core.Time;

public interface ITimeParsingService
{
    TimeParseResult Parse(string input, string? userLocale);
}
