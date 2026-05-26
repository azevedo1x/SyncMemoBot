namespace SyncMemoBot.Discord;

public sealed class DiscordOptions
{
    public const string SectionName = "Discord";

    public string Token { get; init; } = "";

    /// <summary>
    /// When set, slash commands register to this single guild for instant updates (dev workflow).
    /// When null/0, they register globally (can take up to an hour to propagate).
    /// </summary>
    public ulong? DevGuildId { get; init; }
}
