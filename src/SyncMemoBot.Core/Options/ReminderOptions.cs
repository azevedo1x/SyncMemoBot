namespace SyncMemoBot.Core.Options;

public sealed class ReminderOptions
{
    public const string SectionName = "Reminder";

    public string TimeZone { get; set; } = "America/Sao_Paulo";
}
