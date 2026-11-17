namespace Elysium.Classic.Shared.Grid;

/// <summary>
/// A map made of tiles that are either walkable or blocked, with the tiles where players enter.
/// Immutable once created.
/// </summary>
public sealed class GridMap
{
    /// <summary>The largest width or height a map can have (positions travel as 16-bit numbers).</summary>
    public const int MaxSize = ushort.MaxValue;

    private readonly bool[] _blocked;

    public GridMap(int width, int height, IReadOnlyCollection<TilePosition> blockedTiles, IReadOnlyList<TilePosition> spawnPoints)
    {
        ArgumentNullException.ThrowIfNull(blockedTiles);
        ArgumentNullException.ThrowIfNull(spawnPoints);
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(width, MaxSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(height, MaxSize);

        Width = width;
        Height = height;
        _blocked = new bool[width * height];
        foreach (var tile in blockedTiles)
        {
            if (!Contains(tile))
            {
                throw new ArgumentException($"Blocked tile {tile} is outside the map.", nameof(blockedTiles));
            }

            _blocked[Index(tile)] = true;
        }

        if (spawnPoints.Count == 0)
        {
            throw new ArgumentException("A map needs at least one spawn point.", nameof(spawnPoints));
        }

        foreach (var spawn in spawnPoints)
        {
            if (!IsWalkable(spawn))
            {
                throw new ArgumentException($"Spawn point {spawn} is outside the map or blocked.", nameof(spawnPoints));
            }
        }

        SpawnPoints = [.. spawnPoints];
    }

    public int Width { get; }

    public int Height { get; }

    public IReadOnlyList<TilePosition> SpawnPoints { get; }

    public bool Contains(TilePosition tile) => tile.X >= 0 && tile.Y >= 0 && tile.X < Width && tile.Y < Height;

    public bool IsWalkable(TilePosition tile) => Contains(tile) && !_blocked[Index(tile)];

    private int Index(TilePosition tile) => (tile.Y * Width) + tile.X;
}
