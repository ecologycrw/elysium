using Elysium.Classic.Server.Maps;
using Elysium.Classic.Shared.Grid;
using Elysium.Core;

namespace Elysium.Classic.Server.Tests.Maps;

/// <summary>
/// Small maps and rules shared by the map tests.
/// </summary>
internal static class TestMaps
{
    public const int ViewRadius = 5;
    public const int TicksPerStep = 3;
    public const int MaxPathLength = 40;

    public static readonly EntityId Ana = new(1);
    public static readonly EntityId Bruno = new(2);
    public static readonly EntityId Caio = new(3);

    /// <summary>
    /// 40 x 12, all walkable except a wall at column 20 (rows 0 to 9) and a closed box around (35, 1).
    /// Spawns 0 and 1 are next to each other; spawn 2 is far away from both.
    /// </summary>
    public const string Open = """
        spawn 2 5
        spawn 4 5
        spawn 30 5
        tiles
        ....................#.............###...
        ....................#.............#.#...
        ....................#.............###...
        ....................#...................
        ....................#...................
        ....................#...................
        ....................#...................
        ....................#...................
        ....................#...................
        ....................#...................
        ........................................
        ........................................
        """;

    public static MapRules Rules => new(ViewRadius, TicksPerStep, MaxPathLength);

    public static MapSimulation Create(RecordingOutbox outbox) =>
        new(GridMapText.Parse(Open), Rules, outbox);
}
