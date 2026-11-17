using Elysium.Classic.Shared.Grid;
using Elysium.Core;

namespace Elysium.Classic.Server.Maps;

/// <summary>
/// The state of one character on a map. Only the map simulation that owns it reads or changes it (INV-03).
/// </summary>
internal sealed class MapEntity
{
    public MapEntity(EntityId id, string name, TilePosition position)
    {
        Id = id;
        Name = name;
        Position = position;
    }

    public EntityId Id { get; }

    public string Name { get; }

    public TilePosition Position { get; set; }

    /// <summary>The other entities this one can see. Visibility is symmetric: if A sees B, B sees A.</summary>
    public SortedSet<EntityId> Visible { get; } = [];

    /// <summary>The steps still to walk, in order.</summary>
    public Queue<TilePosition> Path { get; } = new();

    /// <summary>The first tick in which this entity may take its next step.</summary>
    public long NextStepTick { get; set; }
}
