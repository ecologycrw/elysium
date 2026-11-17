using Elysium.Classic.Client.World;

namespace Elysium.Bot;

/// <summary>
/// The bot's own words for each change in its world: one line for the terminal, or null when the change is
/// not worth a line (steps happen several times per second).
/// </summary>
public static class BotNarration
{
    public static string? Describe(WorldChange change) => change.Kind switch
    {
        WorldChangeKind.EnteredMap => $"entered the map at {change.Position}",
        WorldChangeKind.EntityAppeared => $"sees {change.Name} at {change.Position}",
        WorldChangeKind.EntityDisappeared => $"lost sight of {change.Name}",
        WorldChangeKind.MoveRefused => $"could not walk to {change.Position}: {change.RefusalReason}",
        _ => null,
    };
}
