using Elysium.Classic.Shared.Grid;

namespace Elysium.Demo.Shared;

/// <summary>
/// The numbers of the demonstration game that the server and the client must agree on. They live here, in the
/// game, and not in the engine (INV-10).
/// </summary>
public static class DemoWorld
{
    public const int DefaultPort = 7000;

    public const int TicksPerSecond = 20;

    /// <summary>One step every 5 ticks: 4 tiles per second at 20 ticks per second.</summary>
    public const int TicksPerStep = 5;

    /// <summary>How long one step takes: the client slides a character from tile to tile in this time.</summary>
    public static TimeSpan StepDuration { get; } = TimeSpan.FromSeconds((double)TicksPerStep / TicksPerSecond);

    /// <summary>Reads the demo map, which is built into the program.</summary>
    public static GridMap LoadMap()
    {
        using var stream = typeof(DemoWorld).Assembly.GetManifestResourceStream("Elysium.Demo.Shared.Data.demo.map")
            ?? throw new InvalidOperationException("The demo map is missing from the program.");
        using var reader = new StreamReader(stream);
        return GridMapText.Parse(reader.ReadToEnd());
    }
}
