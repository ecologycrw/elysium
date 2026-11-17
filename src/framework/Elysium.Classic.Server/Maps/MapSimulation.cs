using Elysium.Classic.Shared.Grid;
using Elysium.Classic.Shared.Messages;
using Elysium.Core;
using Elysium.Core.Server.Sessions;

namespace Elysium.Classic.Server.Maps;

/// <summary>
/// The rules of one grid map: who is on it, where they enter, and how they walk.
/// Who sees whom is delegated to <see cref="AreaOfInterest"/>, and paths to <see cref="GridPathfinder"/>.
/// Single-threaded and deterministic (INV-03, INV-04): time is the number of ticks run so far,
/// and every loop goes in entity ID order. It knows nothing about the network; it only queues
/// messages in the <see cref="IClientOutbox"/>.
/// </summary>
public sealed class MapSimulation
{
    private readonly GridMap _map;
    private readonly MapRules _rules;
    private readonly IClientOutbox _outbox;
    private readonly SortedDictionary<EntityId, MapEntity> _entities = [];
    private readonly AreaOfInterest _areaOfInterest;
    private readonly List<TilePosition> _pathBuffer = [];

    public MapSimulation(GridMap map, MapRules rules, IClientOutbox outbox)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(outbox);
        _map = map;
        _rules = rules;
        _outbox = outbox;
        _areaOfInterest = new AreaOfInterest(rules.ViewRadius, _entities, outbox);
    }

    /// <summary>How many ticks have run. This is the simulation's only notion of time.</summary>
    public long CurrentTick { get; private set; }

    public int PlayerCount => _entities.Count;

    /// <summary>
    /// Puts a character on the map. Players enter at spawn point (players on the map) modulo (number of spawn points).
    /// </summary>
    public void Join(EntityId id, string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (_entities.ContainsKey(id))
        {
            throw new InvalidOperationException($"Entity {id} is already on the map.");
        }

        var spawn = _map.SpawnPoints[_entities.Count % _map.SpawnPoints.Count];
        var entity = new MapEntity(id, name, spawn);
        _entities.Add(id, entity);

        _outbox.Send(id, new EnteredMap(id, spawn));
        _areaOfInterest.Enter(entity);
    }

    /// <summary>Takes a character off the map. Everyone who saw it is told it disappeared.</summary>
    public void Leave(EntityId id)
    {
        if (_entities.Remove(id, out var entity))
        {
            _areaOfInterest.Leave(entity);
        }
    }

    /// <summary>Where the character is now, if it is on the map.</summary>
    public bool TryGetPosition(EntityId id, out TilePosition position)
    {
        if (_entities.TryGetValue(id, out var entity))
        {
            position = entity.Position;
            return true;
        }

        position = default;
        return false;
    }

    /// <summary>
    /// The player asks to walk to <paramref name="destination"/>. The server checks it, finds the path and
    /// replaces any path in progress; the character then walks one tile per step interval, in <see cref="Tick"/>.
    /// A refused request is answered with <see cref="MoveRejected"/>, and the character stops where it is.
    /// </summary>
    public void RequestMove(EntityId id, TilePosition destination)
    {
        if (!_entities.TryGetValue(id, out var entity))
        {
            return;
        }

        entity.Path.Clear();
        if (PlanPath(entity.Position, destination) is { } refused)
        {
            _outbox.Send(id, new MoveRejected(destination, refused));
            return;
        }

        foreach (var step in _pathBuffer)
        {
            entity.Path.Enqueue(step);
        }
    }

    /// <summary>
    /// Runs one tick: every character with a path whose step interval has passed takes exactly one step.
    /// </summary>
    public void Tick()
    {
        CurrentTick++;
        foreach (var entity in _entities.Values)
        {
            if (entity.Path.Count > 0 && CurrentTick >= entity.NextStepTick)
            {
                TakeStep(entity);
            }
        }
    }

    /// <summary>
    /// Fills <see cref="_pathBuffer"/> with the steps from <paramref name="from"/> to <paramref name="destination"/>,
    /// or returns why the destination is refused.
    /// </summary>
    private MoveRejectReason? PlanPath(TilePosition from, TilePosition destination)
    {
        if (!_map.Contains(destination))
        {
            return MoveRejectReason.OutOfMap;
        }

        if (!_map.IsWalkable(destination))
        {
            return MoveRejectReason.Blocked;
        }

        return GridPathfinder.TryFindPath(_map, from, destination, _rules.MaxPathLength, _pathBuffer)
            ? null
            : MoveRejectReason.Unreachable;
    }

    private void TakeStep(MapEntity entity)
    {
        var from = entity.Position;
        entity.Position = entity.Path.Dequeue();
        entity.NextStepTick = CurrentTick + _rules.TicksPerStep;

        _outbox.Send(entity.Id, new EntityMoved(entity.Id, entity.Position));
        _areaOfInterest.Step(entity, from);
    }
}
