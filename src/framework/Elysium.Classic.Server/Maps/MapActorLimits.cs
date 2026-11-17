namespace Elysium.Classic.Server.Maps;

/// <summary>
/// How much work the network can give a map actor, so one client can never freeze the map for everyone.
/// Each game decides the numbers (INV-10).
/// </summary>
public sealed record MapActorLimits
{
    public MapActorLimits(int maxCommandsPerTick, int maxPendingFramesPerPlayer)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCommandsPerTick, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxPendingFramesPerPlayer, 1);
        MaxCommandsPerTick = maxCommandsPerTick;
        MaxPendingFramesPerPlayer = maxPendingFramesPerPlayer;
    }

    /// <summary>A tick takes at most this many commands from the mailbox; the rest wait for the next tick.</summary>
    public int MaxCommandsPerTick { get; }

    /// <summary>A player whose messages waiting in the mailbox go over this number is disconnected.</summary>
    public int MaxPendingFramesPerPlayer { get; }
}
