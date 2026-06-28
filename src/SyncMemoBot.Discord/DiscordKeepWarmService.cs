using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace SyncMemoBot.Discord;

public sealed class DiscordKeepWarmService(
    DiscordSocketClient client,
    DiscordReadinessSignal readiness,
    IOptions<DiscordOptions> options,
    ILogger<DiscordKeepWarmService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(4);

    private readonly DiscordSocketClient _client = client;
    private readonly DiscordReadinessSignal _readiness = readiness;
    private readonly IOptions<DiscordOptions> _options = options;
    private readonly ILogger<DiscordKeepWarmService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Value.Token))
            return;

        await _readiness.WaitUntilReadyAsync(Timeout.InfiniteTimeSpan, stoppingToken);

        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await _client.Rest.GetCurrentUserAsync();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Keep-warm ping failed");
            }
        }
    }
}
