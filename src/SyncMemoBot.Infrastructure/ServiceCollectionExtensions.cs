using Hangfire;
using Hangfire.Storage.SQLite;
using Microsoft.Extensions.DependencyInjection;
using SyncMemoBot.Core.Localization;
using SyncMemoBot.Core.Scheduling;
using SyncMemoBot.Core.Time;
using SyncMemoBot.Infrastructure.Localization;
using SyncMemoBot.Infrastructure.Scheduling;
using SyncMemoBot.Infrastructure.Time;

namespace SyncMemoBot.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSyncMemoBotInfrastructure(
        this IServiceCollection services,
        string hangfireConnectionString = "Data Source=hangfire.db")
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IReminderTimeZone, ConfiguredReminderTimeZone>();
        services.AddSingleton<ITimeParsingService, MultilingualTimeParser>();
        services.AddSingleton<ILocalizedMessages, LocalizedMessages>();
        services.AddSingleton<IReminderScheduler, HangfireReminderScheduler>();
        services.AddSingleton<HangfireJobInvoker>();

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
