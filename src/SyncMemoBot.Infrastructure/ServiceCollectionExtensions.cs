using Hangfire;
using Hangfire.Storage.SQLite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SyncMemoBot.Core.Localization;
using SyncMemoBot.Core.Options;
using SyncMemoBot.Core.RateLimiting;
using SyncMemoBot.Core.Scheduling;
using SyncMemoBot.Core.Time;
using SyncMemoBot.Infrastructure.Localization;
using SyncMemoBot.Infrastructure.RateLimiting;
using SyncMemoBot.Infrastructure.Scheduling;
using SyncMemoBot.Infrastructure.Time;

namespace SyncMemoBot.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSyncMemoBotInfrastructure(
        this IServiceCollection services,
        string hangfireConnectionString = "hangfire.db",
        string rateLimitDatabasePath = "reminders.db")
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IReminderTimeZone, ConfiguredReminderTimeZone>();
        services.AddSingleton<ITimeParsingService, MultilingualTimeParser>();
        services.AddSingleton<ILocalizedMessages, LocalizedMessages>();
        services.AddSingleton<IReminderScheduler, HangfireReminderScheduler>();
        services.AddSingleton<HangfireJobInvoker>();
        services.AddSingleton<IReminderRateLimiter>(sp => new SqliteReminderRateLimiter(
            rateLimitDatabasePath,
            sp.GetRequiredService<IClock>(),
            sp.GetRequiredService<IOptions<RateLimitOptions>>()));

        services.AddHangfire(cfg => cfg
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSQLiteStorage(hangfireConnectionString));

        services.AddHangfireServer(opts =>
        {
            opts.WorkerCount = Math.Max(2, Environment.ProcessorCount);
        });

        return services;
    }
}
