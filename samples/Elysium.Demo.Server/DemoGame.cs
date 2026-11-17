using Elysium.Classic.Server.Maps;
using Elysium.Classic.Shared.Grid;
using Elysium.Core.Server.Sessions;

namespace Elysium.Demo.Server;

/// <summary>
/// The numbers of the demonstration game. They live here, in the game, and not in the engine (INV-10).
/// </summary>
public static class DemoGame
{
    public const int DefaultPort = 7000;

    public const int TicksPerSecond = 20;

    /// <summary>One step every 5 ticks: 4 tiles per second at 20 ticks per second.</summary>
    public static MapRules MapRules { get; } = new(viewRadius: 8, ticksPerStep: 5, maxPathLength: 64);

    /// <summary>A normal client sends at most one move request per tick; 16 waiting messages leaves room for bursts.</summary>
    public static MapActorLimits MapActorLimits { get; } = new(maxCommandsPerTick: 2000, maxPendingFramesPerPlayer: 16);

    public static GatewayOptions GatewayOptions { get; } = new(maxFrameBodySize: 512, maxNameLength: 16);

    /// <summary>Reads the demo map, which is built into the program.</summary>
    public static GridMap LoadMap()
    {
        using var stream = typeof(DemoGame).Assembly.GetManifestResourceStream("Elysium.Demo.Server.Data.demo.map")
            ?? throw new InvalidOperationException("The demo map is missing from the program.");
        using var reader = new StreamReader(stream);
        return GridMapText.Parse(reader.ReadToEnd());
    }
}
