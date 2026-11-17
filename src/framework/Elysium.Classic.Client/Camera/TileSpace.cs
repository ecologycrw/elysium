using System.Numerics;
using Elysium.Classic.Shared.Grid;

namespace Elysium.Classic.Client.Camera;

/// <summary>
/// Where tiles are in the 3D world: the ground is the plane Y = 0, a tile's column runs along X and its row
/// along Z, and each tile is one unit wide, centered on its coordinates (tile 3 spans 2.5 to 3.5).
/// </summary>
public static class TileSpace
{
    /// <summary>The point on the ground for a position in tiles (X = column, Y = row, with fractions).</summary>
    public static Vector3 ToWorld(Vector2 tiles) => new(tiles.X, 0, tiles.Y);

    /// <summary>The center of <paramref name="tile"/> on the ground.</summary>
    public static Vector3 ToWorld(TilePosition tile) => new(tile.X, 0, tile.Y);

    /// <summary>The tile that contains a point on the ground.</summary>
    public static TilePosition ToTile(Vector3 ground) =>
        new((int)MathF.Floor(ground.X + 0.5f), (int)MathF.Floor(ground.Z + 0.5f));
}
