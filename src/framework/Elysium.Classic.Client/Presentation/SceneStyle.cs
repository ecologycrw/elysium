using System.Numerics;
using Elysium.Core.Client.Rendering;

namespace Elysium.Classic.Client.Presentation;

/// <summary>
/// How the world looks: colors, sizes and light. The game decides every value (INV-10).
/// </summary>
public sealed record SceneStyle
{
    /// <summary>The color of walkable ground.</summary>
    public required ColorRgb Ground { get; init; }

    /// <summary>The color of blocked tiles, drawn as boxes standing on the ground.</summary>
    public required ColorRgb Blocked { get; init; }

    /// <summary>How tall a blocked tile is, in tiles. Greater than zero.</summary>
    public required float BlockedHeight
    {
        get;
        init => field = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(BlockedHeight), value, "A blocked tile must stand above the ground.");
    }

    /// <summary>The color of the player's own character.</summary>
    public required ColorRgb Self { get; init; }

    /// <summary>The color of every other character.</summary>
    public required ColorRgb Others { get; init; }

    /// <summary>The width (X), height (Y) and depth (Z) of a character standing on its tile, in tiles. All greater than zero.</summary>
    public required Vector3 CharacterSize
    {
        get;
        init => field = value is { X: > 0, Y: > 0, Z: > 0 } ? value : throw new ArgumentOutOfRangeException(nameof(CharacterSize), value, "A character needs a size in every direction.");
    }

    /// <summary>The color of the mark on the destination the player clicked, while the character walks there.</summary>
    public required ColorRgb Destination { get; init; }

    /// <summary>The color of the mark when the server refused that destination.</summary>
    public required ColorRgb RefusedDestination { get; init; }

    /// <summary>How long the refused mark stays on screen. Greater than zero.</summary>
    public required TimeSpan RefusedDestinationTime
    {
        get;
        init => field = value > TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException(nameof(RefusedDestinationTime), value, "The refusal must show for some time.");
    }

    public required DirectionalLight Light { get; init; }

    /// <summary>The color where nothing is drawn.</summary>
    public required ColorRgb Background { get; init; }
}
