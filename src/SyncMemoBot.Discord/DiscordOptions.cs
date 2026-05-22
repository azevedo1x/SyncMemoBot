namespace SyncMemoBot.Discord;

public sealed class DiscordOptions
{
    public const string SectionName = "Discord";

    public string Token { get; set; } = "";

    // When set, slash commands register to this single guild for instant updates (dev workflow).
    // When null/0, they register globally (can take up to an hour to propagate).
    public ulong? DevGuildId { get; set; }
}
