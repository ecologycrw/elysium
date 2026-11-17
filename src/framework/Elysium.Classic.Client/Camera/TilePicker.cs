using System.Numerics;
using Elysium.Classic.Shared.Grid;

namespace Elysium.Classic.Client.Camera;

/// <summary>Turns a click into the tile of the map under it.</summary>
public static class TilePicker
{
    /// <summary>
    /// The tile of <paramref name="map"/> under the pixel <paramref name="screen"/>. False for the sky and for
    /// points outside the map. Whether the tile can be walked to is the server's decision (INV-01).
    /// </summary>
    public static bool TryPickTile(ThreeQuarterCamera camera, Vector2 screen, GridMap map, out TilePosition tile)
    {
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(map);
        if (camera.TryPickGround(screen, out var ground) && map.Contains(TileSpace.ToTile(ground)))
        {
            tile = TileSpace.ToTile(ground);
            return true;
        }

        tile = default;
        return false;
    }
}
