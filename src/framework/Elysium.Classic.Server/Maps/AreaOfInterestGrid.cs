using Elysium.Classic.Shared.Grid;
using Elysium.Core;

namespace Elysium.Classic.Server.Maps;

/// <summary>
/// Spatial index of the entities on a map: the map is split into square cells as wide as the view radius,
/// so everything a character can see is in its own cell or in one of the 8 cells around it.
/// </summary>
internal sealed class AreaOfInterestGrid
{
    private readonly int _cellSize;
    private readonly Dictionary<(int X, int Y), SortedSet<EntityId>> _cells = [];

    public AreaOfInterestGrid(int cellSize)
    {
        _cellSize = cellSize;
    }

    public void Add(EntityId id, TilePosition position)
    {
        var key = KeyOf(position);
        if (!_cells.TryGetValue(key, out var cell))
        {
            cell = [];
            _cells.Add(key, cell);
        }

        cell.Add(id);
    }

    public void Remove(EntityId id, TilePosition position)
    {
        var key = KeyOf(position);
        if (_cells.TryGetValue(key, out var cell))
        {
            cell.Remove(id);
            if (cell.Count == 0)
            {
                _cells.Remove(key);
            }
        }
    }

    public void Move(EntityId id, TilePosition from, TilePosition to)
    {
        if (KeyOf(from) != KeyOf(to))
        {
            Remove(id, from);
            Add(id, to);
        }
    }

    /// <summary>Adds to <paramref name="result"/> every entity in the cells that can hold something within the view radius.</summary>
    public void Candidates(TilePosition position, SortedSet<EntityId> result)
    {
        var (cellX, cellY) = KeyOf(position);
        for (var dy = -1; dy <= 1; dy++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                if (_cells.TryGetValue((cellX + dx, cellY + dy), out var cell))
                {
                    result.UnionWith(cell);
                }
            }
        }
    }

    private (int X, int Y) KeyOf(TilePosition position) => (position.X / _cellSize, position.Y / _cellSize);
}
