namespace SyncMemoBot.Core.Reminders;

public sealed record ScheduledReminder(
    Guid Id,
    ReminderTarget Target,
    string Message,
    DateTimeOffset ScheduledAtUtc,
    string? Locale);
