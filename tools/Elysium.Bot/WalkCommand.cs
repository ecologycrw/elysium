using Elysium.Classic.Shared.Grid;

namespace Elysium.Bot;

/// <summary>
/// The 'walk' command: logs in one bot, asks to walk to a tile and waits until it arrives or the server refuses.
/// </summary>
public static class WalkCommand
{
    private static readonly TimeSpan EnterTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan WalkTimeout = TimeSpan.FromSeconds(60);

    /// <summary>Returns true when the bot arrived at <paramref name="destination"/>.</summary>
    public static async Task<bool> RunAsync(
        string host, int port, string name, TilePosition destination, TextWriter output, CancellationToken cancellationToken)
    {
        await using var bot = await HeadlessBot.ConnectAsync(host, port, name, output, cancellationToken).ConfigureAwait(false);
        if (!await HeadlessBot.WaitUntilAsync(() => bot.Position is not null, EnterTimeout, cancellationToken).ConfigureAwait(false))
        {
            bot.Say("did not enter the map");
            return false;
        }

        await bot.WalkToAsync(destination, cancellationToken).ConfigureAwait(false);
        await HeadlessBot.WaitUntilAsync(
            () => bot.Position == destination || bot.MovesRefused > 0 || !bot.IsConnected,
            WalkTimeout,
            cancellationToken).ConfigureAwait(false);

        var arrived = bot.Position == destination;
        bot.Say(arrived ? $"arrived at {destination}" : $"stopped at {bot.Position}");
        return arrived;
    }
}
