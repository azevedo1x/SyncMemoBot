using Microsoft.Extensions.Options;
using SyncMemoBot.Core.Options;
using SyncMemoBot.Core.Time;

namespace SyncMemoBot.Infrastructure.Time;

public sealed class ConfiguredReminderTimeZone : IReminderTimeZone
{
    public TimeZoneInfo Zone { get; }

    public ConfiguredReminderTimeZone(IOptions<ReminderOptions> options)
    {
        Zone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);
    }
}
