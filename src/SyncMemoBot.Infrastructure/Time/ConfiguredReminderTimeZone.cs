using Microsoft.Extensions.Options;
using SyncMemoBot.Core.Options;
using SyncMemoBot.Core.Time;

namespace SyncMemoBot.Infrastructure.Time;

public sealed class ConfiguredReminderTimeZone(IOptions<ReminderOptions> options) : IReminderTimeZone
{
    public TimeZoneInfo Zone { get; } = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);
}
