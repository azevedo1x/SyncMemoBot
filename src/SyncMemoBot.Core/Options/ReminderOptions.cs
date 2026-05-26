namespace SyncMemoBot.Core.Options;

public sealed class ReminderOptions
{
    public const string SectionName = "Reminder";

    public string TimeZone { get; init; } = "America/Sao_Paulo";
}
