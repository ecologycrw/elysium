using System.Numerics;
using Elysium.Classic.Shared.Grid;

namespace Elysium.Classic.Client.Camera;

/// <summary>Turns a click into the tile of the map under it.</summary>
public static class TilePicker
{
    /// <summary>
    /// The tile of <paramref name="map"/> under the pixel <paramref name="screen"/>, with every tile flat on the
    /// ground. False for the sky and for points outside the map. Whether the tile can be walked to is the
    /// server's decision (INV-01).
    /// </summary>
    public static bool TryPickTile(ThreeQuarterCamera camera, Vector2 screen, GridMap map, out TilePosition tile) =>
        TryPickTile(camera, screen, map, blockedHeight: 0, out tile);

    /// <summary>
    /// The tile of <paramref name="map"/> under the pixel <paramref name="screen"/>, where blocked tiles are boxes
    /// <paramref name="blockedHeight"/> tall and the rest is flat ground: a click on the top or the side of a
    /// box picks the blocked tile, not the ground behind it. False for the sky and for points outside the map.
    /// </summary>
    public static bool TryPickTile(ThreeQuarterCamera camera, Vector2 screen, GridMap map, float blockedHeight, out TilePosition tile)
    {
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentOutOfRangeException.ThrowIfNegative(blockedHeight);
        if (!camera.TryPickGround(screen, out var ground))
        {
            tile = default;
            return false;
        }

        if (blockedHeight > 0
            && camera.TryPickAtHeight(screen, blockedHeight, out var top)
            && TryFindBlockedTile(map, top, ground, out tile))
        {
            return true;
        }

        tile = TileSpace.ToTile(ground);
        return map.Contains(tile);
    }

    /// <summary>
    /// Walks, nearest to the camera first, the tiles the ray crosses while it comes down from the top of the
    /// boxes to the ground. The ray stays between the ground and the top of the boxes the whole way, so the
    /// first blocked tile it enters is the box it hits.
    /// </summary>
    private static bool TryFindBlockedTile(GridMap map, Vector3 top, Vector3 ground, out TilePosition blocked)
    {
        // A tile spans half a unit around its center: shifted by half a unit, a tile is the floor of the point.
        var start = new Vector2(top.X + 0.5f, top.Z + 0.5f);
        var end = new Vector2(ground.X + 0.5f, ground.Z + 0.5f);
        var column = new GridAxisWalk(start.X, end.X);
        var row = new GridAxisWalk(start.Y, end.Y);
        var crossings = column.Crossings + row.Crossings;
        for (var crossed = 0; ; crossed++)
        {
            var tile = new TilePosition(column.Cell, row.Cell);
            if (map.Contains(tile) && !map.IsWalkable(tile))
            {
                blocked = tile;
                return true;
            }

            if (crossed == crossings)
            {
                blocked = default;
                return false;
            }

            if (column.NextBorder < row.NextBorder)
            {
                column.Cross();
            }
            else
            {
                row.Cross();
            }
        }
    }

    /// <summary>
    /// One axis of a walk along a segment through grid cells one unit wide: the cell it is in, and how far along
    /// the segment (from 0 at the start to 1 at the end) it next crosses into another cell.
    /// </summary>
    private struct GridAxisWalk
    {
        private readonly int _step;
        private readonly float _cellLength;

        public GridAxisWalk(float start, float end)
        {
            var length = end - start;
            Cell = (int)MathF.Floor(start);
            Crossings = Math.Abs((int)MathF.Floor(end) - Cell);
            _step = Math.Sign(length);
            _cellLength = _step == 0 ? float.PositiveInfinity : 1 / MathF.Abs(length);
            NextBorder = _step switch
            {
                > 0 => (Cell + 1 - start) / length,
                < 0 => (start - Cell) / -length,
                _ => float.PositiveInfinity,
            };
        }

        public int Cell { get; private set; }

        public int Crossings { get; }

        public float NextBorder { get; private set; }

        public void Cross()
        {
            Cell += _step;
            NextBorder += _cellLength;
        }
    }
}
