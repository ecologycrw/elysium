using Elysium.Classic.Server.Maps;
using Elysium.Classic.Shared.Grid;
using Elysium.Core.Server.Sessions;
using Elysium.Demo.Shared;

namespace Elysium.Demo.Server;

/// <summary>
/// The numbers of the demonstration game that only the server needs. The ones the client shares (the map and
/// the speed of a step) are in <see cref="DemoWorld"/>. They live in the game, and not in the engine (INV-10).
/// </summary>
public static class DemoGame
{
    public const int DefaultPort = DemoWorld.DefaultPort;

    public const int TicksPerSecond = DemoWorld.TicksPerSecond;

    /// <summary>One step every <see cref="DemoWorld.TicksPerStep"/> ticks.</summary>
    public static MapRules MapRules { get; } = new(viewRadius: 8, ticksPerStep: DemoWorld.TicksPerStep, maxPathLength: 64);

    /// <summary>A normal client sends at most one move request per tick; 16 waiting messages leaves room for bursts.</summary>
    public static MapActorLimits MapActorLimits { get; } = new(maxCommandsPerTick: 2000, maxPendingFramesPerPlayer: 16);

    public static GatewayOptions GatewayOptions { get; } = new(maxFrameBodySize: 512, maxNameLength: 16, maxQueuedFramesPerClient: 1024);

    /// <summary>Reads the demo map, the same one the client reads.</summary>
    public static GridMap LoadMap() => DemoWorld.LoadMap();
}
