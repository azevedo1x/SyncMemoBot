using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using SyncMemoBot.Discord.Modules;
using SyncMemoBot.Infrastructure.Time;

namespace SyncMemoBot.Discord;

public sealed class DiscordClientHost(
    DiscordSocketClient client,
    InteractionService interactions,
    DiscordReadinessSignal readiness,
    IServiceProvider services,
    IOptions<DiscordOptions> options,
    ILogger<DiscordClientHost> logger) : IHostedService
{
    private readonly DiscordSocketClient _client = client;
    private readonly InteractionService _interactions = interactions;
    private readonly DiscordReadinessSignal _readiness = readiness;
    private readonly IServiceProvider _services = services;
    private readonly IOptions<DiscordOptions> _options = options;
    private readonly ILogger<DiscordClientHost> _logger = logger;
    private bool _commandsRegistered;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        MultilingualTimeParser.WarmUp();

        var token = _options.Value.Token;
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Discord:Token is not configured — bot will NOT connect. Set via user-secrets in dev or DISCORD__TOKEN env var in prod.");
            return;
        }

        _client.Log += ForwardLog;
        _interactions.Log += ForwardLog;
        _client.Ready += OnReadyAsync;
        _client.InteractionCreated += OnInteractionCreated;

        await _interactions.AddModulesAsync(typeof(ReminderCommandsModule).Assembly, _services);

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_client.LoginState == LoginState.LoggedIn)
        {
            await _client.LogoutAsync();
            await _client.StopAsync();
        }
    }

    private async Task OnReadyAsync()
    {
        _readiness.SignalReady();

        if (_commandsRegistered) return;

        try
        {
            if (_options.Value.DevGuildId is { } guildId && guildId != 0)
            {
                await _interactions.RegisterCommandsToGuildAsync(guildId, deleteMissing: true);
                _logger.LogInformation("Registered slash commands to guild {GuildId}", guildId);
            }
            else
            {
                await _interactions.RegisterCommandsGloballyAsync(deleteMissing: true);
                _logger.LogInformation("Registered slash commands globally (may take up to 1 hour to propagate)");
            }

            await WarmUpRestPipelineAsync();

            _commandsRegistered = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register slash commands");
        }
    }

    private async Task WarmUpRestPipelineAsync()
    {
        try
        {
            await _client.Rest.GetUserAsync(_client.CurrentUser.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "REST pipeline warm-up failed");
        }
    }

    private Task OnInteractionCreated(SocketInteraction interaction)
    {
        _ = Task.Run(() => HandleInteractionAsync(interaction));
        return Task.CompletedTask;
    }

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        try
        {
            if (interaction.Type == InteractionType.ApplicationCommand)
                await DeferEarlyAsync(interaction);

            var context = new SocketInteractionContext(_client, interaction);
            var result = await _interactions.ExecuteCommandAsync(context, _services);

            if (!result.IsSuccess)
                _logger.LogWarning("Interaction failed: {Error} — {ErrorReason}", result.Error, result.ErrorReason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error handling interaction");
        }
    }

    private async Task DeferEarlyAsync(SocketInteraction interaction)
    {
        try
        {
            await interaction.DeferAsync(ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to defer interaction early");
        }
    }

    private Task ForwardLog(LogMessage msg)
    {
        var level = msg.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error    => LogLevel.Error,
            LogSeverity.Warning  => LogLevel.Warning,
            LogSeverity.Info     => LogLevel.Information,
            LogSeverity.Verbose  => LogLevel.Debug,
            LogSeverity.Debug    => LogLevel.Trace,
            _                    => LogLevel.Information
        };
        _logger.Log(level, msg.Exception, "[{Source}] {Message}", msg.Source, msg.Message);
        return Task.CompletedTask;
    }
}
