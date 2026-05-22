using Discord.Interactions;
using Discord.WebSocket;
using Hangfire;
using Serilog;
using SyncMemoBot.Core.Dispatch;
using SyncMemoBot.Core.Options;
using SyncMemoBot.Discord;
using SyncMemoBot.Discord.Dispatch;
using SyncMemoBot.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/bot-.log", rollingInterval: RollingInterval.Day));

builder.Services
    .Configure<ReminderOptions>(builder.Configuration.GetSection(ReminderOptions.SectionName))
    .Configure<DiscordOptions>(builder.Configuration.GetSection(DiscordOptions.SectionName));

builder.Services.AddSyncMemoBotInfrastructure(
    builder.Configuration.GetConnectionString("Hangfire") ?? "Data Source=hangfire.db");

builder.Services.AddSingleton(new DiscordSocketConfig
{
    GatewayIntents = global::Discord.GatewayIntents.Guilds | global::Discord.GatewayIntents.DirectMessages,
    LogLevel = global::Discord.LogSeverity.Info
});
builder.Services.AddSingleton<DiscordSocketClient>();
builder.Services.AddSingleton(sp =>
    new InteractionService(sp.GetRequiredService<DiscordSocketClient>()));
builder.Services.AddSingleton<IReminderDispatcher, DiscordReminderDispatcher>();
builder.Services.AddHostedService<DiscordClientHost>();

var app = builder.Build();

app.UseHangfireDashboard("/hangfire");
app.MapGet("/", () => "SyncMemoBot is running. Hangfire dashboard at /hangfire");

app.Run();
