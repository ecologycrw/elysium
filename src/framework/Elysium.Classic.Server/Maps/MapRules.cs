namespace Elysium.Classic.Server.Maps;

/// <summary>
/// The numbers of a map that each game decides (INV-10): how far characters see,
/// how fast they walk and how long a path can be.
/// </summary>
public sealed record MapRules
{
    public MapRules(int viewRadius, int ticksPerStep, int maxPathLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(viewRadius, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(ticksPerStep, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxPathLength, 1);
        ViewRadius = viewRadius;
        TicksPerStep = ticksPerStep;
        MaxPathLength = maxPathLength;
    }

    /// <summary>A character sees everything within this many tiles, counting diagonals as one.</summary>
    public int ViewRadius { get; }

    /// <summary>A character takes one step at most every this many ticks.</summary>
    public int TicksPerStep { get; }

    /// <summary>The longest path, in steps, that one move request can produce.</summary>
    public int MaxPathLength { get; }
}
